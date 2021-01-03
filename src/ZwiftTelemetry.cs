using ZwiftPacketMonitor;

namespace ZwiftTelemetryBrowserSource
{
    public class ZwiftTelemetry {
        
        public PlayerState PlayerState {get; internal set;}

        public void UpdatePlayerState(PlayerState newState)
        {
            if ((PlayerState == null) || (PlayerState.Id == newState.Id))
            {
                PlayerState = newState;
            }
        }
    }
}