using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nito.AsyncEx;

namespace ZwiftTelemetryBrowserSource.Services
{
    /// <summary>
    /// This service is responsible for calculating average power, speed, cadence, and heartrate. As incoming telemetry data is
    /// received from Zwift, it gets proceed by this service through the LogTelemetry method. The basic
    /// algorithm is that if the rider is currently *moving*, average power will be calculated (including
    /// zeroes). If the rider is not moving, the average power calculation is essentially paused until such
    /// a time that they begin moving again. All other metrics are recorded and calculated regardless of movement.
    /// In order to guard against performance degredations with longer running activities, incoming telemetry data will be 
    /// normalized down to one data point per second.
    /// </summary>
    public class AverageTelemetryService : BackgroundService
    {
        private ILogger<AverageTelemetryService> Logger;
        private HashSet<AvgPowerData> PowerData;
        private HashSet<AvgSpeedData> SpeedData;
        private HashSet<AvgCadenceData> CadenceData;
        private HashSet<AvgHeartrateData> HeartrateData;

        private IList<AvgPowerData> IntermediatePowerData;
        private IList<AvgSpeedData> IntermediateSpeedData;
        private IList<AvgCadenceData> IntermediateCadenceData;
        private IList<AvgHeartrateData> IntermediateHeartrateData;

        public AvgSummary AvgSummary;

        private AsyncLock AsyncLock;

        public AverageTelemetryService(ILogger<AverageTelemetryService> logger) 
        {
            Logger = logger;
            PowerData = new HashSet<AvgPowerData>();
            SpeedData = new HashSet<AvgSpeedData>();
            CadenceData = new HashSet<AvgCadenceData>();
            HeartrateData = new HashSet<AvgHeartrateData>();

            IntermediatePowerData = new List<AvgPowerData>();
            IntermediateSpeedData = new List<AvgSpeedData>();
            IntermediateCadenceData = new List<AvgCadenceData>();
            IntermediateHeartrateData = new List<AvgHeartrateData>();

            AvgSummary = new AvgSummary();
            AsyncLock = new AsyncLock();
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            try {
                Logger.LogInformation("Starting AverageTelemetryService");

                await Task.Run(async () => 
                {
                    // Loop until the service gets the shutdown signal
                    while (!cancellationToken.IsCancellationRequested)
                    {
                        try {
                            IList<AvgPowerData> ipd;
                            IList<AvgSpeedData> isd;
                            IList<AvgCadenceData> icd;
                            IList<AvgHeartrateData> ihd;

                            // Since there are multiple threads trying to
                            // update these objects we need to lock here so we
                            // can copy and clear. We'll then do some processing
                            // on the copy
                            using (AsyncLock.Lock())
                            {
                                // Make a copy and clear the original. This
                                // prevents us from accumulating this intermediate
                                // data unbounded in memory
                                ipd = IntermediatePowerData.ToList();
                                IntermediatePowerData = new List<AvgPowerData>();

                                isd = IntermediateSpeedData.ToList();
                                IntermediateSpeedData = new List<AvgSpeedData>();

                                icd = IntermediateCadenceData.ToList();
                                IntermediateCadenceData = new List<AvgCadenceData>();

                                ihd = IntermediateHeartrateData.ToList();
                                IntermediateHeartrateData = new List<AvgHeartrateData>();
                            }

                            // Loop through the intermedia data sets, normalize them down 
                            // to one data point per second add to the overall dataset so we can calculate averages
                            foreach (var p in ipd)
                            {
                                if (!PowerData.Contains(p))
                                {
                                    PowerData.Add(p);
                                }
                            }

                            foreach (var s in isd) 
                            {
                                if (!SpeedData.Contains(s))
                                {
                                    SpeedData.Add(s);
                                }
                            }

                            foreach (var c in icd) 
                            {
                                if (!CadenceData.Contains(c))
                                {
                                    CadenceData.Add(c);
                                }
                            }

                            foreach (var h in ihd) 
                            {
                                if (!HeartrateData.Contains(h))
                                {
                                    HeartrateData.Add(h);
                                }
                            }

                            // Now recalculate the averages
                            if (PowerData.Count() > 0)
                            {
                                AvgSummary.Power = PowerData.Sum(x => x.Power) / PowerData.Count();
                            }

                            if (SpeedData.Count() > 0)
                            {
                                AvgSummary.Speed = SpeedData.Sum(x => x.Speed) / SpeedData.Count();
                            }

                            if (CadenceData.Count() > 0)
                            {
                                AvgSummary.Cadence = CadenceData.Sum(x => x.Cadence) / CadenceData.Count();
                            }

                            if (HeartrateData.Count() > 0)
                            {
                                AvgSummary.Heartrate = HeartrateData.Sum(x => x.Heartrate) / HeartrateData.Count();
                            }

                            // We'll recalculate averages ever 5 seconds
                            await Task.Delay(5000, cancellationToken);
                        }
                        catch (TaskCanceledException) {}
                        catch (Exception e) {
                            Logger.LogError(e, "Task.Run()");
                        }
                    }
                }, cancellationToken);

                Logger.LogInformation("Stopping AveragePowerService");                
            }
            catch (Exception ex) {
                Logger.LogError(ex, "ExecuteAsync");
            }
        }

        /// <summary>
        /// Resets the average calculations. This can be useful when the rider switches between events.
        /// </summary>
        public void Reset()
        {
            Logger.LogDebug("Resetting telemetry");
            
            using (AsyncLock.Lock())
            {
                AvgSummary = new AvgSummary();
                PowerData.Clear();
                SpeedData.Clear();
                CadenceData.Clear();
                HeartrateData.Clear();
            }
        }

        /// <summary>
        /// Takes incoming telemetry data and adds it to the calculation to computer averages.
        /// </summary>
        /// <param name="state">The incoming rider telemetry</param>
        /// <returns>The currently computed averages</returns>
        public AvgSummary LogTelemetry(ZwiftPacketMonitor.PlayerState state) 
        {
            using (AsyncLock.Lock())
            {
                // Only calculating avgs if the player is moving
                if (state.Speed > 0)
                {
                    IntermediatePowerData.Add(new AvgPowerData() { Power = state.Power });
                    IntermediateHeartrateData.Add(new AvgHeartrateData() { Heartrate = state.Heartrate });

                    // convert speed from mm/hr to mi/hr
                    IntermediateSpeedData.Add(new AvgSpeedData() { Speed = (int)(state.Speed / 1609000) });
                    
                    // convert cadence from uHz to rpm
                    IntermediateCadenceData.Add(new AvgCadenceData() { Cadence = (int)(state.CadenceUHz * 0.00006) });
                }
            }

            return (AvgSummary);
        }
    }

    public class AvgSummary 
    {
        public int Power {get; set;}
        public double Speed {get; set;}
        public int Cadence {get; set;}
        public int Heartrate {get; set;}
    }

    public abstract class AverageTelemetryData 
    {
        public AverageTelemetryData() {
            // Normalize this data point down to a representation of the number of seconds elapsed since the epoch
            Timecode = (int)DateTime.Now.Subtract(DateTime.UnixEpoch).TotalSeconds;
        }

        public int Timecode {get; set;}

        public virtual string Name {get;}

        protected int Metric {get; set;}

        public override int GetHashCode()
        {
            return Timecode.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return ((obj as AverageTelemetryData).Timecode == Timecode);
        }

        public override string ToString()
        {
            return $"Time: {Timecode}, {Name}: {Metric}";
        }

    }

    public class AvgPowerData : AverageTelemetryData
    {
        public int Power {get => Metric; set { Metric = value; }}

        public override string Name => "Power";
    }

    public class AvgSpeedData : AverageTelemetryData
    {
        public int Speed {get => Metric; set { Metric = value; }}

        public override string Name => "Speed";
    }

    public class AvgCadenceData : AverageTelemetryData
    {
        public int Cadence {get => Metric; set { Metric = value; }}

        public override string Name => "Cadence";
    }

        public class AvgHeartrateData : AverageTelemetryData
    {
        public int Heartrate {get => Metric; set { Metric = value; }}

        public override string Name => "HR";
    }
}