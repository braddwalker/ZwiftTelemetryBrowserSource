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
    public class AveragePowerService : BackgroundService
    {
        private ILogger<AveragePowerService> Logger;
        private HashSet<AvgPowerData> PowerData;

        private IList<AvgPowerData> IntermediatePowerData;
        private int AveragePower;

        private AsyncAutoResetEvent AsyncAutoResetEvent;

        public AveragePowerService(ILogger<AveragePowerService> logger) {
            Logger = logger;
            PowerData = new HashSet<AvgPowerData>();
            IntermediatePowerData = new List<AvgPowerData>();
            AveragePower = 0;
            AsyncAutoResetEvent = new AsyncAutoResetEvent(false);
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            try {
                Logger.LogInformation("Starting AveragePowerService");

                await Task.Run(async () => 
                {
                    // Loop until the service gets the shutdown signal
                    while (!cancellationToken.IsCancellationRequested)
                    {
                        try {
                            IList<AvgPowerData> ipd;

                            // Since there are multiple threads trying to
                            // update this object we need to lock here so we
                            // can copy and clear. We'll then do some processing
                            // on the copy
                            lock (IntermediatePowerData)
                            {
                                // Make a copy and clear the original. This
                                // prevents us from accumulating this intermediate
                                // data unbounded in memory
                                ipd = IntermediatePowerData;
                                IntermediatePowerData = new List<AvgPowerData>();
                            }

                            // Loop through the intermedia power data
                            // and normalize it down to one data point per second
                            // Then add to the overall dataset so we can calculate power
                            foreach (var p in ipd)
                            {
                                if (!PowerData.Contains(p))
                                {
                                    PowerData.Add(p);
                                }
                            }

                            if (PowerData.Count() > 0)
                            {
                                AveragePower = PowerData.Sum(x => x.Power) / PowerData.Count();
                            }

                            // Wait here until new data arrives to calculate
                            await AsyncAutoResetEvent.WaitAsync(cancellationToken);
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

        public int LogPower(ZwiftPacketMonitor.PlayerState state) {
            // Only calculating avg based on actual *moving time* power
            if (state.Speed > 0)
            {
                IntermediatePowerData.Add(new AvgPowerData() { Power = state.Power });
                
                // Signal to the background thread that new data has arrived
                AsyncAutoResetEvent.Set();
            }

            return (AveragePower);
        }
    }

    public class AvgPowerData
    {
        public AvgPowerData() {
            Timecode = (int)DateTime.Now.Subtract(DateTime.UnixEpoch).TotalSeconds;
        }

        public int Timecode {get; set;}
        public int Power {get; set;}

        public override int GetHashCode()
        {
            return Timecode.GetHashCode();
        }

        public override string ToString()
        {
            return $"Time: {Timecode}, Power: {Power}";
        }

        public override bool Equals(object obj)
        {
            return ((obj as AvgPowerData).Timecode == Timecode);
        }
    }
}