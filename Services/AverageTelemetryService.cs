using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
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
        private ILogger<AverageTelemetryService> _logger;
        private HashSet<AvgPowerData> _powerData;
        private HashSet<AvgSpeedData> _speedData;
        private HashSet<AvgCadenceData> _cadenceData;
        private HashSet<AvgHeartrateData> _heartrateData;

        private IList<AvgPowerData> _intermediatePowerData;
        private IList<AvgSpeedData> _intermediateSpeedData;
        private IList<AvgCadenceData> _intermediateCadenceData;
        private IList<AvgHeartrateData> _intermediateHeartrateData;
        private EventService _eventService;

        public AvgSummary _avgSummary;
        private AsyncLock _asyncLock;

        public AverageTelemetryService(ILogger<AverageTelemetryService> logger, EventService eventService) 
        {
            _logger = logger ?? throw new ArgumentException(nameof(logger));
            _eventService = eventService ?? throw new ArgumentException(nameof(eventService));

            _eventService.EventChanged += (s, e) =>
            {
                // If we are entering an event, aways reset average telemetry.
                // If leaving an event (NewEventId =0) we won't reset.
                if (e.NewEventId != 0)
                {
                    Reset();
                }
            };

            _powerData = new HashSet<AvgPowerData>();
            _speedData = new HashSet<AvgSpeedData>();
            _cadenceData = new HashSet<AvgCadenceData>();
            _heartrateData = new HashSet<AvgHeartrateData>();

            _intermediatePowerData = new List<AvgPowerData>();
            _intermediateSpeedData = new List<AvgSpeedData>();
            _intermediateCadenceData = new List<AvgCadenceData>();
            _intermediateHeartrateData = new List<AvgHeartrateData>();

            _avgSummary = new AvgSummary();
            _asyncLock = new AsyncLock();
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            try {
                _logger.LogInformation("Starting AverageTelemetryService");

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
                            using (_asyncLock.Lock())
                            {
                                // Make a copy and clear the original. This
                                // prevents us from accumulating this intermediate
                                // data unbounded in memory
                                ipd = _intermediatePowerData.ToList();
                                _intermediatePowerData = new List<AvgPowerData>();

                                isd = _intermediateSpeedData.ToList();
                                _intermediateSpeedData = new List<AvgSpeedData>();

                                icd = _intermediateCadenceData.ToList();
                                _intermediateCadenceData = new List<AvgCadenceData>();

                                ihd = _intermediateHeartrateData.ToList();
                                _intermediateHeartrateData = new List<AvgHeartrateData>();
                            }

                            // Loop through the intermedia data sets, normalize them down 
                            // to one data point per second add to the overall dataset so we can calculate averages
                            foreach (var p in ipd)
                            {
                                if (!_powerData.Contains(p))
                                {
                                    _powerData.Add(p);
                                }
                            }

                            foreach (var s in isd) 
                            {
                                if (!_speedData.Contains(s))
                                {
                                    _speedData.Add(s);
                                }
                            }

                            foreach (var c in icd) 
                            {
                                if (!_cadenceData.Contains(c))
                                {
                                    _cadenceData.Add(c);
                                }
                            }

                            foreach (var h in ihd) 
                            {
                                if (!_heartrateData.Contains(h))
                                {
                                    _heartrateData.Add(h);
                                }
                            }

                            // Now recalculate the averages
                            if (_powerData.Count() > 0)
                            {
                                _avgSummary.Power = _powerData.Sum(x => x.Power) / _powerData.Count();
                            }

                            if (_speedData.Count() > 0)
                            {
                                _avgSummary.Speed = _speedData.Sum(x => x.Speed) / _speedData.Count();
                            }

                            if (_cadenceData.Count() > 0)
                            {
                                _avgSummary.Cadence = _cadenceData.Sum(x => x.Cadence) / _cadenceData.Count();
                            }

                            if (_heartrateData.Count() > 0)
                            {
                                _avgSummary.Heartrate = _heartrateData.Sum(x => x.Heartrate) / _heartrateData.Count();
                            }

                            // We'll recalculate averages ever 5 seconds
                            await Task.Delay(5000, cancellationToken);
                        }
                        catch (TaskCanceledException) {}
                        catch (Exception e) {
                            _logger.LogError(e, "Task.Run()");
                        }
                    }
                }, cancellationToken);

                _logger.LogInformation("Stopping AveragePowerService");                
            }
            catch (Exception ex) {
                _logger.LogError(ex, "ExecuteAsync");
            }
        }

        /// <summary>
        /// Resets the average calculations. This can be useful when the rider switches between events.
        /// </summary>
        private void Reset()
        {
            _logger.LogDebug("Resetting telemetry");
            
            using (_asyncLock.Lock())
            {
                _avgSummary = new AvgSummary();
                _powerData.Clear();
                _speedData.Clear();
                _cadenceData.Clear();
                _heartrateData.Clear();
            }
        }

        /// <summary>
        /// Takes incoming telemetry data and adds it to the calculation to computer averages.
        /// </summary>
        /// <param name="state">The incoming rider telemetry</param>
        /// <returns>The currently computed averages</returns>
        public AvgSummary LogTelemetry(ZwiftPacketMonitor.PlayerState state) 
        {
            using (_asyncLock.Lock())
            {
                // Only calculating avgs if the player is moving
                if (state.Speed > 0)
                {
                    _intermediatePowerData.Add(new AvgPowerData() { Power = state.Power });
                    _intermediateHeartrateData.Add(new AvgHeartrateData() { Heartrate = state.Heartrate });

                    // convert speed from mm/hr to mi/hr
                    _intermediateSpeedData.Add(new AvgSpeedData() { Speed = (int)(state.Speed / 1609000) });
                    
                    // convert cadence from uHz to rpm
                    _intermediateCadenceData.Add(new AvgCadenceData() { Cadence = (int)(state.CadenceUHz * 0.00006) });
                }
            }

            return (_avgSummary);
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