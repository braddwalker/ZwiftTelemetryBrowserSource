namespace ZwiftTelemetryBrowserSource.Services.Twitch
{
    public class TwitchConfig
    {
        public bool Enabled {get; set;}
        public string AuthTokenFile {get; set;}
        public string ChannelName {get; set;}
        public string IrcServer {get; set;}
        public int IrcPort {get; set;}

        public string Username {get; set;}
    }
}