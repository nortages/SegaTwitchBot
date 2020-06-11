using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace SegaTwitchBot
{
    static class TwitchInfo
    {
        //private static IConfiguration _configuration;

        static TwitchInfo()
        {            
            string pathToConfig = "config.json";

            if (File.Exists(pathToConfig))
            {
                using StreamReader r = new StreamReader(pathToConfig);
                string json = r.ReadToEnd();
                var items = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);

                ChannelName = items["ChannelName"];
                BotUsername = items["BotUsername"];
                BotToken = items["BotToken"];
                ClientID = items["ClientID"];
                RefreshToken = items["RefreshToken"];
                JsonBinSecret = items["JsonBinSecret"];
                GoogleApiKey = items["GoogleApiKey"];
            }
            else
            {
                ChannelName = Environment.GetEnvironmentVariable("ChannelName");
                BotUsername = Environment.GetEnvironmentVariable("BotUsername");
                BotToken = Environment.GetEnvironmentVariable("BotToken");
                ClientID = Environment.GetEnvironmentVariable("ClientID");
                RefreshToken = Environment.GetEnvironmentVariable("RefreshToken");
                JsonBinSecret = Environment.GetEnvironmentVariable("JsonBinSecret");
                GoogleApiKey = Environment.GetEnvironmentVariable("GoogleApiKey");
            }
        }

        public static string ChannelName { get; private set; }
        public static string BotUsername { get; private set; }
        public static string BotToken { get; private set; }
        public static string ClientID { get; private set; }
        public static string RefreshToken { get; private set; }
        public static string JsonBinSecret { get; private set; }
        public static string GoogleApiKey { get; private set; }
    }
}
