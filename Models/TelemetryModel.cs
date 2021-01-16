namespace ZwiftTelemetryBrowserSource.Models
{
    public class TelemetryModel {
        public int PlayerId {get; set;}
        public int Power {get; set;}
        public int HeartRate {get; set;}

        public int AvgPower {get; set;}
        public int AvgHeartRate {get; set;}
        public int AvgCadence {get; set;}
        public double AvgSpeed {get; set;}
    }
}