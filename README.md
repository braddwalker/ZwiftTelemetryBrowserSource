# ZwiftTelemetryBrowserSource
This project implements a small .NET web server that can be used to render custom telemetry for the Zwift cycling simulator in the form of an OBS or X-Split browser source. The telemetry can then be used to overlay your Zwift scene, adding a bit of dynamic, visual content to your stream. Currently this browser source includes two "speedometer" type data gauges that show your current power and HR on a scale using zones that you can configure.

**NOTE**: Because this utilizes a network packet capture to intercept the UDP packets from the Zwift game, your system may require this code to run using elevated privileges.

## Usage
---
```
dotnet run
```
