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
            if (!_config.Enabled)
            {
                return;
            }

            _raceData = new Dictionary<int, PlayerRaceData>();

            if (eventId.HasValue)
            {
                _config.EventId = eventId.Value;
            }
        }

        public void RegisterRider(int riderId, string name)
        {
            if (!_config.Enabled)
            {
                return;
            }

            if (!_riders.Any(x => x.RiderId == riderId))
            {
                _riders.Add(new PlayerData() { RiderId = riderId, Name = name });
            }
        }

        public void RegisterResults(PlayerState state)
        {
            if (!_config.Enabled)
            {
                return;
            }

            // Only do this if there is some config defined and we've matched the specific event
            if (_config.EventId == state.GroupId)
            {
                var showResults = false;

                // Do we already have an entry for this player
                if (_raceData.ContainsKey(state.Id))
                {
                    var x = _raceData[state.Id];

                    // Only show results if someone has gone to the next lap
                    showResults = showResults || (x.Laps != state.Laps);

                    x.WorldTime = state.WorldTime;
                    x.ElapsedTime = state.Time;
                    x.Laps = state.Laps;
                }
                else
                {
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
            var results = _raceData.Values.Where(x => x.Laps > 0).OrderByDescending(x => x.Laps).ThenBy(x => x.WorldTime);
            var position = 1;
            var leaderTime = results.FirstOrDefault().WorldTime;
            var output = new StringBuilder();

            output.AppendLine();
            output.AppendLine("Position  Lap  Time         Diff     World Time     Rider");
            foreach (var player in results)
            {
                var et = TimeSpan.FromSeconds(player.ElapsedTime);
                var etFormatted = string.Format("{0:D1}h:{1:D2}m:{2:D2}", et.Hours, et.Minutes, et.Seconds);
                
                var diffFormatted = "";
                if (position > 1)
                {
                    var diff = TimeSpan.FromMilliseconds(player.WorldTime - leaderTime);
                    if (diff.TotalMinutes < 1)
                    {
                        diffFormatted = string.Format("+{0:D1}.{1:D3}s", diff.Seconds, diff.Milliseconds);
                    }
                    else
                    {
                        diffFormatted = string.Format("+{0:D2}:{1:D2}", diff.Minutes, diff.Seconds);
                    }
                }

                output.AppendLine(String.Format("{0, -8}  {1, 3}  {2, -9}  {3, 8}  {4, -13}  {5, -20}", position++, player.Laps, etFormatted, diffFormatted, player.WorldTime, GetRiderName(player.RiderId)));
            }
            
            Console.WriteLine($"{output.ToString()}");
        }
    }
}