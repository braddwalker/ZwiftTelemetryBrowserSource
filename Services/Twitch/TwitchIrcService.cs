namespace ZwiftTelemetryBrowserSource.Services.Twitch
{
    using System;
    using System.Net.Sockets;
    using System.IO;
    using Microsoft.Extensions.Options;
    using Microsoft.Extensions.Logging;

    public class TwitchIrcService
    {
        public string userName;
        private string channel;

        private TcpClient _tcpClient;
        private StreamReader _inputStream;
        private StreamWriter _outputStream;

        private TwitchConfig _twitchConfig;
        private string _twitchOAuthKey;
        private ILogger<TwitchIrcService> Logger;

        public TwitchIrcService(ILogger<TwitchIrcService> logger, IOptions<TwitchConfig> twitchConfig)
        {
            _twitchConfig = twitchConfig.Value;
            Logger = logger;

            if (_twitchConfig.Enabled) 
            {
                Logger.LogInformation("Twitch IRC service enabled");
                _twitchOAuthKey = File.ReadAllText(_twitchConfig.AuthTokenFile).Trim();
                Logger.LogInformation($"Twitch OAuth key loaded from {new FileInfo(_twitchConfig.AuthTokenFile).FullName}");

                try
                {
                    this.userName = _twitchConfig.ChannelName;
                    this.channel = _twitchConfig.ChannelName.ToLower();

                    _tcpClient = new TcpClient(_twitchConfig.IrcServer, _twitchConfig.IrcPort);
                    _inputStream = new StreamReader(_tcpClient.GetStream());
                    _outputStream = new StreamWriter(_tcpClient.GetStream());

                    // Try to join the room
                    _outputStream.WriteLine("PASS " + _twitchOAuthKey);
                    _outputStream.WriteLine("NICK " + userName);
                    _outputStream.WriteLine("USER " + userName + " 8 * :" + userName);
                    _outputStream.WriteLine("JOIN #" + channel);
                    _outputStream.Flush();
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "TwitchIrcService");
                }
            }
        }

        public void SendIrcMessage(string message)
        {
            if (!_twitchConfig.Enabled)
                return;

            Logger.LogDebug($"SendIrcMessage: {message}");

            try
            {
                _outputStream.WriteLine(message);
                _outputStream.Flush();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "SendIrcMessage");
            }
        }

        public void SendPublicChatMessage(string message)
        {
            if (!_twitchConfig.Enabled)
                return;

            Logger.LogDebug($"SendPublicChatMessage: {message}");

            try
            {
                SendIrcMessage(":" + userName + "!" + userName + "@" + userName +
                ".tmi.twitch.tv PRIVMSG #" + channel + " :" + message);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "SendPublicChatMessage");
            }
        }

        public string ReadMessage()
        {
            if (!_twitchConfig.Enabled)
                return null;

            try
            {
                string message = _inputStream.ReadLine();
                return message;
            }
            catch (Exception ex)
            {
                return "Error receiving message: " + ex.Message;
            }
        }
    }
}