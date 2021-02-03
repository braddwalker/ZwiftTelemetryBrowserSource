using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Microsoft.Extensions.Configuration;
using ZwiftPacketMonitor;
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
        public int Laps {get; set;}
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

        private IDictionary<int, PlayerRaceData> _raceData;
        private IList<PlayerData> _riders;

        public ResultsService(ILogger<ResultsService> logger, IOptions<ResultsConfig> config, IConfiguration rootConfig)
        {
            _logger = logger ?? throw new ArgumentException(nameof(logger));
            _config = config?.Value ?? throw new ArgumentException(nameof(config));
            _raceData = new Dictionary<int, PlayerRaceData>();
            _riders = new List<PlayerData>();

            if (_config.Enabled)
            {
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
            _raceData = new Dictionary<int, PlayerRaceData>();

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
            if ((_config.Enabled) && (_config.EventId == state.GroupId))
            {
                var showResults = false;
                //_logger.LogDebug($"FINISH LINE: {state}");

                // Do we already have an entry for this player
                if (_raceData.ContainsKey(state.Id))
                {
                    //_logger.LogDebug("EXISTING RIDER");
                    var x = _raceData[state.Id];

                    showResults = showResults || (x.Laps != state.Laps);

                    x.WorldTime = state.WorldTime;
                    x.ElapsedTime = state.Time;
                    x.Laps = state.Laps;
                }
                else
                {
                    //_logger.LogDebug("NEW RIDER");

                    _raceData.Add(state.Id, new PlayerRaceData()
                    {
                        RiderId = state.Id,
                        WorldTime = state.WorldTime,
                        ElapsedTime = state.Time,
                        Laps = state.Laps
                    });
                }

                if (showResults)
                {
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
            output.AppendLine("Position  Lap  Time      World Time     Rider");
            foreach (var player in _raceData.Values.Where(x => x.Laps > 0).OrderByDescending(x => x.Laps).ThenBy(x => x.WorldTime))
            {
                var t = TimeSpan.FromSeconds(player.ElapsedTime);
                var elapsedTime = string.Format("{0:D1}h:{1:D2}m:{2:D2}", t.Hours, t.Minutes, t.Seconds);

                output.AppendLine(String.Format("{0, -8}  {1, -3}  {2, -8}  {3, -13}  {4, -20}", position++, player.Laps, elapsedTime, player.WorldTime, GetRiderName(player.RiderId)));
            }
            
            Console.WriteLine($"{output.ToString()}");
        }
    }
}