# ZwiftTelemetryBrowserSource
This project implements a small .NET web server that can be used to render a custom view of your realtime Zwift HR and power data in the form of an OBS or X-Split browser source. The program will intercept the UDP packets sent between the Zwift game and remote servers, and parse the telemetry data from those packets. The telemetry can then be added to your Zwift scene, adding dynamic, visual content to your stream. Currently this browser source includes a "speedometer" type data gauge that shows, **in realtime**, your current power and HR on a scale based on your actual performance zones.

![Telemetry gauge](https://github.com/braddwalker/ZwiftTelemetryBrowserSource/blob/main/docs/images/zwift-example-animated.gif?raw=true)

## Prerequisites
---
* [Microsoft .NET 5.0](https://dotnet.microsoft.com/download/dotnet/5.0)
* [Npcap](https://nmap.org/download.html) (Windows only)

## Configuration
---
Configuration for the app is handled by the `appsettings.json` file.
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft": "Warning",
      "Microsoft.Hosting.Lifetime": "Information",
      "ZwiftPacketMonitor": "Information"
    }
  },
  "NetworkInterface": "",
  "Urls": "http://*:89",
  "ResetAveragesOnEventFinish": false,

  "Alerts": {
    "RideOn": {
      "Enabled": true,
      "AudioSource": "/audio/rockon.ogg"
    },
    "Chat": {
      "Enabled": true,
      "AlertOwnMessages": true,
      "AlertOtherEvents": false,
      "ShowProfileImage": true
    }
  },

  "Speech": {
    "Enabled": true,
    "SubscriptionKeyFile": "azure-key.txt",
    "Region": "eastus",
    "DefaultVoiceName": "en-GB-MiaNeural",
    "Voices": [
      { "Country": "EG", "VoiceName": "ar-EG-SalmaNeural"},
      { "Country": "EG", "VoiceName": "ar-EG-ShakirNeural"},
      { "Country": "SA", "VoiceName": "ar-SA-ZariyahNeural"},
      { "Country": "SA", "VoiceName": "ar-SA-HamedNeural"},
      { "Country": "BG", "VoiceName": "bg-BG-KalinaNeural"},
      { "Country": "BG", "VoiceName": "bg-BG-BorislavNeural"},
      { "Country": "ES", "VoiceName": "ca-ES-AlbaNeural"},
      { "Country": "ES", "VoiceName": "ca-ES-JoanaNeural"},
      { "Country": "ES", "VoiceName": "ca-ES-EnricNeural"},
      { "Country": "HK", "VoiceName": "zh-HK-HiuGaaiNeural"},
      { "Country": "HK", "VoiceName": "zh-HK-HiuMaanNeural"},
      { "Country": "HK", "VoiceName": "zh-HK-WanLungNeural"},
      { "Country": "CN", "VoiceName": "zh-CN-XiaoxiaoNeural"},
      { "Country": "CN", "VoiceName": "zh-CN-XiaoyouNeural"},
      { "Country": "CN", "VoiceName": "zh-CN-YunyangNeural"},
      { "Country": "CN", "VoiceName": "zh-CN-YunyeNeural"},
      { "Country": "TW", "VoiceName": "zh-TW-HsiaoChenNeural"},
      { "Country": "TW", "VoiceName": "zh-TW-HsiaoYuNeural"},
      { "Country": "TW", "VoiceName": "zh-TW-YunJheNeural"},
      { "Country": "HR", "VoiceName": "hr-HR-GabrijelaNeural"},
      { "Country": "HR", "VoiceName": "hr-HR-SreckoNeural"},
      { "Country": "CZ", "VoiceName": "cs-CZ-VlastaNeural"},
      { "Country": "CZ", "VoiceName": "cs-CZ-AntoninNeural"},
      { "Country": "DK", "VoiceName": "da-DK-ChristelNeural"},
      { "Country": "DK", "VoiceName": "da-DK-JeppeNeural"},
      { "Country": "NL", "VoiceName": "nl-NL-ColetteNeural"},
      { "Country": "NL", "VoiceName": "nl-NL-FennaNeural"},
      { "Country": "NL", "VoiceName": "nl-NL-MaartenNeural"},
      { "Country": "AU", "VoiceName": "en-AU-NatashaNeural"},
      { "Country": "AU", "VoiceName": "en-AU-WilliamNeural"},
      { "Country": "CA", "VoiceName": "en-CA-ClaraNeural"},
      { "Country": "CA", "VoiceName": "en-CA-LiamNeural"},
      { "Country": "IN", "VoiceName": "en-IN-NeerjaNeural"},
      { "Country": "IN", "VoiceName": "en-IN-PrabhatNeural"},
      { "Country": "IE", "VoiceName": "en-IE-EmilyNeural"},
      { "Country": "IE", "VoiceName": "en-IE-ConnorNeural"},
      { "Country": "GB", "VoiceName": "en-GB-LibbyNeural"},
      { "Country": "GB", "VoiceName": "en-GB-MiaNeural"},
      { "Country": "GB", "VoiceName": "en-GB-RyanNeural"},
      { "Country": "US", "VoiceName": "en-US-AriaNeural"},
      { "Country": "US", "VoiceName": "en-US-JennyNeural"},
      { "Country": "US", "VoiceName": "en-US-GuyNeural"},
      { "Country": "FI", "VoiceName": "fi-FI-NooraNeural"},
      { "Country": "FI", "VoiceName": "fi-FI-SelmaNeural"},
      { "Country": "FI", "VoiceName": "fi-FI-HarriNeural"},
      { "Country": "CA", "VoiceName": "fr-CA-SylvieNeural"},
      { "Country": "CA", "VoiceName": "fr-CA-JeanNeural"},
      { "Country": "FR", "VoiceName": "fr-FR-DeniseNeural"},
      { "Country": "FR", "VoiceName": "fr-FR-HenriNeural"},
      { "Country": "CH", "VoiceName": "fr-CH-ArianeNeural"},
      { "Country": "CH", "VoiceName": "fr-CH-FabriceNeural"},
      { "Country": "AT", "VoiceName": "de-AT-IngridNeural"},
      { "Country": "AT", "VoiceName": "de-AT-JonasNeural"},
      { "Country": "DE", "VoiceName": "de-DE-KatjaNeural"},
      { "Country": "DE", "VoiceName": "de-DE-ConradNeural"},
      { "Country": "CH", "VoiceName": "de-CH-LeniNeural"},
      { "Country": "CH", "VoiceName": "de-CH-JanNeural"},
      { "Country": "GR", "VoiceName": "el-GR-AthinaNeural"},
      { "Country": "GR", "VoiceName": "el-GR-NestorasNeural"},
      { "Country": "IL", "VoiceName": "he-IL-HilaNeural"},
      { "Country": "IL", "VoiceName": "he-IL-AvriNeural"},
      { "Country": "IN", "VoiceName": "hi-IN-SwaraNeural"},
      { "Country": "IN", "VoiceName": "hi-IN-MadhurNeural"},
      { "Country": "HU", "VoiceName": "hu-HU-NoemiNeural"},
      { "Country": "HU", "VoiceName": "hu-HU-TamasNeural"},
      { "Country": "ID", "VoiceName": "id-ID-GadisNeural"},
      { "Country": "ID", "VoiceName": "id-ID-ArdiNeural"},
      { "Country": "IT", "VoiceName": "it-IT-ElsaNeural"},
      { "Country": "IT", "VoiceName": "it-IT-IsabellaNeural"},
      { "Country": "IT", "VoiceName": "it-IT-DiegoNeural"},
      { "Country": "JP", "VoiceName": "ja-JP-NanamiNeural"},
      { "Country": "JP", "VoiceName": "ja-JP-KeitaNeural"},
      { "Country": "KR", "VoiceName": "ko-KR-SunHiNeural"},
      { "Country": "KR", "VoiceName": "ko-KR-InJoonNeural"},
      { "Country": "MY", "VoiceName": "ms-MY-YasminNeural"},
      { "Country": "MY", "VoiceName": "ms-MY-OsmanNeural"},
      { "Country": "NO", "VoiceName": "nb-NO-IselinNeural"},
      { "Country": "NO", "VoiceName": "nb-NO-PernilleNeural"},
      { "Country": "NO", "VoiceName": "nb-NO-FinnNeural"},
      { "Country": "PL", "VoiceName": "pl-PL-AgnieszkaNeural"},
      { "Country": "PL", "VoiceName": "pl-PL-ZofiaNeural"},
      { "Country": "PL", "VoiceName": "pl-PL-MarekNeural"},
      { "Country": "BR", "VoiceName": "pt-BR-FranciscaNeural"},
      { "Country": "BR", "VoiceName": "pt-BR-AntonioNeural"},
      { "Country": "PT", "VoiceName": "pt-PT-FernandaNeural"},
      { "Country": "PT", "VoiceName": "pt-PT-RaquelNeural"},
      { "Country": "PT", "VoiceName": "pt-PT-DuarteNeural"},
      { "Country": "RO", "VoiceName": "ro-RO-AlinaNeural"},
      { "Country": "RO", "VoiceName": "ro-RO-EmilNeural"},
      { "Country": "RU", "VoiceName": "ru-RU-DariyaNeural"},
      { "Country": "RU", "VoiceName": "ru-RU-SvetlanaNeural"},
      { "Country": "RU", "VoiceName": "ru-RU-DmitryNeural"},
      { "Country": "SK", "VoiceName": "sk-SK-ViktoriaNeural"},
      { "Country": "SK", "VoiceName": "sk-SK-LukasNeural"},
      { "Country": "SI", "VoiceName": "sl-SI-PetraNeural"},
      { "Country": "SI", "VoiceName": "sl-SI-RokNeural"},
      { "Country": "MX", "VoiceName": "es-MX-DaliaNeural"},
      { "Country": "MX", "VoiceName": "es-MX-JorgeNeural"},
      { "Country": "ES", "VoiceName": "es-ES-ElviraNeural"},
      { "Country": "ES", "VoiceName": "es-ES-AlvaroNeural"},
      { "Country": "SE", "VoiceName": "sv-SE-HilleviNeural"},
      { "Country": "SE", "VoiceName": "sv-SE-SofieNeural"},
      { "Country": "SE", "VoiceName": "sv-SE-MattiasNeural"},
      { "Country": "IN", "VoiceName": "ta-IN-PallaviNeural"},
      { "Country": "IN", "VoiceName": "ta-IN-ValluvarNeural"},
      { "Country": "IN", "VoiceName": "te-IN-ShrutiNeural"},
      { "Country": "IN", "VoiceName": "te-IN-MohanNeural"},
      { "Country": "TH", "VoiceName": "th-TH-AcharaNeural"},
      { "Country": "TH", "VoiceName": "th-TH-PremwadeeNeural"},
      { "Country": "TH", "VoiceName": "th-TH-NiwatNeural"},
      { "Country": "TR", "VoiceName": "tr-TR-EmelNeural"},
      { "Country": "TR", "VoiceName": "tr-TR-AhmetNeural"},
      { "Country": "VN", "VoiceName": "vi-VN-HoaiMyNeural"},
      { "Country": "VN", "VoiceName": "vi-VN-NamMinhNeural"},
      { "Country": "CN", "VoiceName": "zh-CN-XiaohanNeural"},
      { "Country": "CN", "VoiceName": "zh-CN-XiaomoNeural"},
      { "Country": "CN", "VoiceName": "zh-CN-XiaoruiNeural"},
      { "Country": "CN", "VoiceName": "zh-CN-XiaoxuanNeural"},
      { "Country": "CN", "VoiceName": "zh-CN-YunxiNeural"},
      { "Country": "EE", "VoiceName": "et-EE-AnuNeural"},
      { "Country": "EE", "VoiceName": "et-EE-KertNeural"},
      { "Country": "IE", "VoiceName": "ga-IE-OrlaNeural"},
      { "Country": "IE", "VoiceName": "ga-IE-ColmNeural"},
      { "Country": "LV", "VoiceName": "lv-LV-EveritaNeural"},
      { "Country": "LV", "VoiceName": "lv-LV-NilsNeural"},
      { "Country": "LT", "VoiceName": "lt-LT-OnaNeural"},
      { "Country": "LT", "VoiceName": "lt-LT-LeonasNeural"},
      { "Country": "MT", "VoiceName": "mt-MT-GraceNeural"},
      { "Country": "MT", "VoiceName": "mt-MT-JosephNeural"}
    ]
  },

  "Twitch": {
    "Enabled": true,
    "AuthTokenFile": "twitch-key.txt",
    "ChannelName": "LookingForWatts",
    "Username": "wattbazooka",
    "IrcServer": "irc.twitch.tv",
    "IrcPort": 6667
  },
  
  "Zones": {
      "HR": {
          "Z1": 126,
          "Z2": 144,
          "Z3": 162,
          "Z4": 180,
          "Z5": 200
      },
      "Power": {
          "Z1": 165,
          "Z2": 224,
          "Z3": 268,
          "Z4": 313,
          "Z5": 356,
          "Z6": 400,
          "Z7": 450
      }
    }
}
```

Setting | Value
------- | ------
NetworkInterface | For Windows, use your computer's IP address. For Mac, use the network interface name (`en0`, etc.)
Urls | The hostname (defaults to any ip or name bound to your local machine) and port number for IIS to listen for requests on.
Zones | Your HR and Power values for each of your respective performance zones. HR zones are in bpm, power zones in watts. Each zone corresponds to a different color slice on the gauge.

## Usage
---
To start the program, simply run the following command.

**NOTE**: Because this program utilizes a network packet capture to intercept the UDP packets from the Zwift game, your system may require this code to run using elevated privileges.

```
dotnet run
```

![Program console](https://github.com/braddwalker/ZwiftTelemetryBrowserSource/blob/main/docs/images/program-console.png?raw=true)

## OBS/X-Split Integration
Since this program implements a small web server, you can use it to add the gauge content to your stream by simply adding a Browser Source to your streaming program of choice, and then size and position the gauge on the screen as desired.

### Power/HR gauges
URL: http://localhost:89/

![OBS configuration](https://github.com/braddwalker/ZwiftTelemetryBrowserSource/blob/main/docs/images/obs-settings.png?raw=true)

### Average speed/power/hr/cadence
URL: http://localhost:89/Averages/All

TODO: Fully document

### RideOn alerts
URL: http://localhost:89/Alerts/RideOn

TODO: Fully document

### Chat TTS
URL: http://localhost:89/Averages/Chat

TODO: Fully document

---
![Stream example](https://github.com/braddwalker/ZwiftTelemetryBrowserSource/blob/main/docs/images/zwift-example.png?raw=true)
