using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ZwiftTelemetryBrowserSource.Services
{
    public class AveragePowerService : BackgroundService
    {
        private ILogger<AveragePowerService> Logger;
        private HashSet<AvgPowerData> PowerData;

        private IList<AvgPowerData> IntermediatePowerData;
        private int AveragePower;

        public AveragePowerService(ILogger<AveragePowerService> logger) {
            Logger = logger;
            PowerData = new HashSet<AvgPowerData>();
            IntermediatePowerData = new List<AvgPowerData>();
            AveragePower = 0;
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            try {
                await Task.Run(async () => 
                {
                    while (!cancellationToken.IsCancellationRequested)
                    {
                        try {
                            // Loop through the intermedia power data
                            // and normalize it down to one data point per second
                            // Then add to the overall dataset so we can calculate power
                            foreach (var p in IntermediatePowerData)
                            {
                                Logger.LogInformation(p.ToString());

                                if (!PowerData.Contains(p))
                                {
                                    PowerData.Add(p);
                                }
                            }

                            if (PowerData.Count() > 0)
                            {
                                AveragePower = PowerData.Sum(x => x.Power) / PowerData.Count();
                                Logger.LogInformation(PowerData.Count().ToString());
                            }

                            await Task.Delay(500, cancellationToken);
                        }
                        catch (Exception e) {
                            Logger.LogError("Task.Run()", e);
                        }
                    }
                }, cancellationToken);                
            }
            catch (Exception ex) {
                Logger.LogError("ExecuteAsync", ex);
            }
        }

        public int LogPower(ZwiftPacketMonitor.PlayerState state) {
            if (state.Speed > 0)
            {
                IntermediatePowerData.Add(new AvgPowerData() { Power = state.Power });
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
    }
}