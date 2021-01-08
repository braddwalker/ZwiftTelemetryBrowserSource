# ZwiftTelemetryBrowserSource
This project implements a small .NET web server that can be used to render custom telemetry for the Zwift cycling simulator in the form of an OBS or X-Split browser source. The program will intercept the UDP packets sent between the Zwift game and remote servers, and parse the telemetry data from those packets. The telemetry can then be added to your Zwift scene, adding dynamic, visual content to your stream. Currently this browser source includes two "speedometer" type data gauges that show, **in realtime**, your current power and HR on a scale based on your actual zones.

![Telemetry gauges](https://github.com/braddwalker/ZwiftTelemetryBrowserSource/blob/main/docs/images/zwift-example-animated.gif?raw=true)

## Configuration
---
Configuration for the app is handled by the `appsettings.json` file.
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "Microsoft.Hosting.Lifetime": "Information"
    }
  },
  "NetworkInterface": "en0",
  "Urls": "http://*:89",
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
NetworkInterface | The name/id of the network interface to capture game packets on. This would be whatever network interface your Zwift game is using to connect to the internet.
Urls | The hostname (defaults to any ip or name bound to your local machine) and port number for IIS to listen for requests on.
Zones | Your HR and Power values for each of your respective performance zones. Each zone corresponds to a different color slice on the gauge.

## Usage
---
To start the web server, simply run the following command.

**NOTE**: Because this program utilizes a network packet capture to intercept the UDP packets from the Zwift game, your system may require this code to run using elevated privileges.

```
dotnet run
```

![Program console](https://github.com/braddwalker/ZwiftTelemetryBrowserSource/blob/main/docs/images/program-console.png?raw=true)

## OBS/X-Split Integration
Since this program implements a small web server, you can use it to add the gauge content to your stream by simply adding a Browser Source to your streaming program of choice, and then size and position the gauge on the screen as desired.

![OBS configuration](https://github.com/braddwalker/ZwiftTelemetryBrowserSource/blob/main/docs/images/obs-settings.png?raw=true)
