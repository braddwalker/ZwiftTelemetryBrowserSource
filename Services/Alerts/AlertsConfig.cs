namespace ZwiftTelemetryBrowserSource.Services.Alerts
{
    public class AlertsConfig
    {
        public ChatConfig Chat {get; set;}
        public RideOnConfig RideOn {get; set;}
    }

    public class ChatConfig
    {
        public bool Enabled {get; set;}
        public bool AlertOwnMessages {get; set;}
        public bool ShowProfileImage {get; set;}
    }

    public class RideOnConfig
    {
        public bool Enabled {get; set;}
        public string AudioSource {get; set;}
    }
}