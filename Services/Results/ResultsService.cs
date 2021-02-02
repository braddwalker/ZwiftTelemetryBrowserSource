using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Microsoft.Extensions.Configuration;
using ZwiftPacketMonitor;
using System.Drawing;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ZwiftTelemetryBrowserSource.Services.Results
{
    class PlayerRaceData
    {
        public int RiderId {get; set;}
        public int ElapsedTime {get; set;}
        public long WorldTime {get; set;}
        public int Passes {get; set;}
    }

    class PlayerData
    {
        public int RiderId {get; set;}
        public string Name {get; set;}
    }

    public class ResultsService
    {
        private ILogger<ResultsService> _logger;
        private ResultsConfig _config;

        private IList<PlayerRaceData> _raceData;
        private IList<PlayerData> _riders;

        public ResultsService(ILogger<ResultsService> logger, IOptions<ResultsConfig> config, IConfiguration rootConfig)
        {
            _logger = logger ?? throw new ArgumentException(nameof(logger));
            _config = config?.Value;
            _raceData = new List<PlayerRaceData>();
            _riders = new List<PlayerData>();

            if (_config != null)
            {
                if (_config.FinishLine.Length < 2)
                {
                    throw new ArgumentException("FinishLine requires at least 2 points");
                }

                _logger.LogDebug($"Finish Line: {_config.FinishLine[0].X}, {_config.FinishLine[0].Y} to {_config.FinishLine[1].X}, {_config.FinishLine[1].Y}");

                // Listen for config changes
                ChangeToken.OnChange(
                    () => rootConfig.GetReloadToken(),
                    () => {
                        var eventId = rootConfig.GetSection("Results").GetValue<int>("EventId");
                        _logger.LogDebug($"Configuration change detected, reloading EventId: {eventId}");
                        Reset(eventId);
                    });
            }
        }

        public void Reset(int? eventId = null)
        {
            _raceData = new List<PlayerRaceData>();

            if (eventId.HasValue)
            {
                _config.EventId = eventId.Value;
            }
        }

        public void RegisterRider(int riderId, string name)
        {
            if (!_riders.Any(x => x.RiderId == riderId))
            {
                _riders.Add(new PlayerData() { RiderId = riderId, Name = name });
                //_logger.LogDebug($"REGISTER: {riderId}, {name}");
            }
        }

        public void RegisterResults(PlayerState state)
        {
            // Only do this if there is some config defined and we've matched the specific event
            if ((_config != null) && (_config.EventId == state.GroupId))
            {
                if ((state.Speed > 0) && PassedFinishLine(state))
                {
                    //_logger.LogDebug($"FINISH LINE: {state}");

                    // Do we already have an entry for this player
                    var x = _raceData.FirstOrDefault(x => x.RiderId == state.Id);
                    if (x != null)
                    {
                        //_logger.LogDebug("EXISTING RIDER");

                        // We have a record for this player, but now we need to check WorldTime - 10000ms due to duplicates from the size of the finish line polygon
                        if (x.WorldTime <= (state.WorldTime - 10000))
                        {
                            _logger.LogDebug("STALE DATA");
                            x.WorldTime = state.WorldTime;
                            x.ElapsedTime = state.Time;
                            ++x.Passes;
                        }
                        else
                        {
                            // Don't show results if we're just filtering out duplicates
                            return;
                        }
                    }
                    else
                    {
                        //_logger.LogDebug("NEW RIDER");

                        _raceData.Add(new PlayerRaceData()
                        {
                            RiderId = state.Id,
                            WorldTime = state.WorldTime,
                            ElapsedTime = state.Time,
                            Passes = 1,
                        });
                    }

                    PrintResults();
                }
            }
        }

        private string GetRiderName(int riderId)
        {
            var rider = _riders.FirstOrDefault(x => x.RiderId == riderId);
            if (rider != null) 
            {
                return ($"{rider.Name} ({riderId})");
            }
            else
            {
                return (riderId.ToString());
            }
        }

        private void PrintResults()
        {
            var position = 1;
            var output = new StringBuilder();

            output.AppendLine();
            output.AppendLine("Position  Lap  Time       Rider");
            foreach (var player in _raceData.OrderByDescending(x => x.Passes).ThenBy(x => x.WorldTime))
            {
                var t = TimeSpan.FromSeconds(player.ElapsedTime);
                var elapsedTime = string.Format("{0:D1}h:{1:D2}m:{2:D2}", t.Hours, t.Minutes, t.Seconds);

                output.AppendLine(String.Format("{0, -8}  {1, -3}  {2, -8}  {3, -20}", position++, player.Passes, elapsedTime, GetRiderName(player.RiderId)));
            }
            
            Console.WriteLine($"{output.ToString()}");
        }

        private bool PassedFinishLine(PlayerState state)
        {
            // At least 1km needs to have elapsed before we
            // will recognize someone passing the finish line
            if (state.Distance <= 1000)
            {
                return (false);
            }

            // This tells us if the player's current position is inside the polygon
            // we've defined around the finish line
            return (IsPointInPolygon4(_config.FinishLine, new PointF(state.X, state.Y)));
        }

        /// <summary>
        /// Determines if the given point is inside the polygon
        /// </summary>
        /// <param name="polygon">the vertices of polygon</param>
        /// <param name="testPoint">the given point</param>
        /// <returns>true if the point is inside the polygon; otherwise, false</returns>
        private static bool IsPointInPolygon4(PointF[] polygon, PointF testPoint)
        {
            bool result = false;
            int j = polygon.Length - 1;
            for (int i = 0; i < polygon.Length; i++)
            {
                if (polygon[i].Y < testPoint.Y && polygon[j].Y >= testPoint.Y || polygon[j].Y < testPoint.Y && polygon[i].Y >= testPoint.Y)
                {
                    if (polygon[i].X + (testPoint.Y - polygon[i].Y) / (polygon[j].Y - polygon[i].Y) * (polygon[j].X - polygon[i].X) < testPoint.X)
                    {
                        result = !result;
                    }
                }
                j = i;
            }
            return result;
        }
    }
}