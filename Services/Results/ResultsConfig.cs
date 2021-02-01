using System.Drawing;

namespace ZwiftTelemetryBrowserSource.Services.Results
{
    public class ResultsConfig
    {
        public int EventId {get; set;}

        public PointF[] FinishLine {get; set;}

        public float Distance {get; set;}
    }
}