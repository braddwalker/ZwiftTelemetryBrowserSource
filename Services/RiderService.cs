using System;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using ZwiftTelemetryBrowserSource.Util;
using System.Threading.Tasks;
using System.Threading;
using Newtonsoft.Json;

namespace ZwiftTelemetryBrowserSource.Services
{
    public class RiderInfo
    {
        public int RiderId {get; set;}
        public string FirstName {get; set;}
        public string LastName {get; set;}
        public string Name => $"{FirstName} {LastName}";
        public string CountryCode {get; set;}

        public override string ToString()
        {
            return (JsonConvert.SerializeObject(this));
        }
    }

    /// <summary>
    /// This class acts as a cache repository for rider information
    /// </summary>
    public class RiderService : BaseZwiftService
    {
        private readonly ILogger<RiderService> _logger;
        private readonly ZwiftMonitorService _zwiftService;
        private Dictionary<int, RiderInfo> _riders;

        public RiderService(ILogger<RiderService> logger, ZwiftMonitorService zwiftService) : base(logger)
        {
            _logger = logger ?? throw new ArgumentException(nameof(logger));
            _zwiftService = zwiftService ?? throw new ArgumentException(nameof(zwiftService));
            _riders = new Dictionary<int, RiderInfo>();
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            _zwiftService.IncomingChatMessageEvent += (s, e) =>
            {
                AddRider(new RiderInfo()
                {
                    RiderId = e.Message.RiderId, 
                    FirstName = e.Message.FirstName, 
                    LastName = e.Message.LastName, 
                    CountryCode = RegionInfo.CurrentRegion.IsoCodeFromNumeric(e.Message.CountryCode)
                });
            };

            _zwiftService.IncomingPlayerEnteredWorldEvent += (s, e) =>
            {
                AddRider(new RiderInfo()
                {
                    RiderId = e.PlayerUpdate.RiderId,
                    FirstName = e.PlayerUpdate.FirstName,
                    LastName = e.PlayerUpdate.LastName
                });
            };

            _zwiftService.IncomingRideOnGivenEvent += (s, e) =>
            {
                AddRider(new RiderInfo()
                {
                    RiderId = e.RideOn.RiderId, 
                    FirstName = e.RideOn.FirstName, 
                    LastName = e.RideOn.LastName, 
                    CountryCode = RegionInfo.CurrentRegion.IsoCodeFromNumeric(e.RideOn.CountryCode)
                });
            };

            await Task.CompletedTask;
        }

        private void AddRider(RiderInfo rider)
        {
            _riders.TryAdd(rider.RiderId, rider);
        }

        public RiderInfo GetRider(int riderId) 
        {
            RiderInfo rider = null;
            _riders.TryGetValue(riderId, out rider);
            return (rider);
        }

        public ICollection<RiderInfo> GetRiders()
        {
            return (_riders.Values.ToList());
        }
    }
}