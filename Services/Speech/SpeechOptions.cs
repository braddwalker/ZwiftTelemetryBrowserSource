namespace ZwiftTelemetryBrowserSource.Services.Speech
{
    public class SpeechOptions
    {
        public bool Enabled {get; set;}
        public string SubscriptionKeyFile {get; set;}
        public string Region {get; set;}
        public string DefaultVoiceName {get; set;}

        public LocaleVoice[] Voices {get; set;}
    }

    public class LocaleVoice
    {
        public string Country {get; set;}
        public string VoiceName {get; set;}
    }
}