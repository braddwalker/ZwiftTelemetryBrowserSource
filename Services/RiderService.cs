using System;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;

namespace ZwiftTelemetryBrowserSource.Services
{
    public class RiderInfo
    {
        public int RiderId {get; set;}
        public string FirstName {get; set;}
        public string LastName {get; set;}
        public string Name => $"{FirstName} {LastName}";
    }

    /// <summary>
    /// This class acts as a cache repository for rider information
    /// </summary>
    public class RiderService
    {
        private ILogger<RiderService> _logger;
        private Dictionary<int, RiderInfo> _riders;

        public RiderService(ILogger<RiderService> logger)
        {
            _logger = logger ?? throw new ArgumentException(nameof(logger));
            _riders = new Dictionary<int, RiderInfo>();
        }

        public void AddRider(int riderId, string firstName, string lastName)
        {
            AddRider(new RiderInfo() 
            {
                RiderId = riderId,
                FirstName = firstName,
                LastName = lastName
            });
        }

        public void AddRider(RiderInfo rider)
        {
            if (!_riders.ContainsKey(rider.RiderId))
            {
                _riders.Add(rider.RiderId, rider);
            }
        }

        public RiderInfo GetRider(int riderId) 
        {
            if (_riders.ContainsKey(riderId))
            {
                return (_riders[riderId]);
            }

            return (null);
        }

        public ICollection<RiderInfo> GetRiders()
        {
            return (_riders.Values.ToList());
        }
    }
}