using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace NortagesTwitchBot
{
    static class TwitchInfo
    {
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
                BotPassword = items["BotPassword"];
                GmailEmail = items["GmailEmail"];
                GmailPassword = items["GmailPassword"];
                BotToken = items["BotToken"];
                ClientID = items["ClientID"];
                RefreshToken = items["RefreshToken"];
                ChannelToken = items["ChannelToken"];
                JsonBinSecret = items["JsonBinSecret"];
                GoogleApiKey = items["GoogleApiKey"];
            }
            else
            {
                ChannelName = Environment.GetEnvironmentVariable("ChannelName");
                BotUsername = Environment.GetEnvironmentVariable("BotUsername");
                BotPassword = Environment.GetEnvironmentVariable("BotPassword");
                GmailEmail = Environment.GetEnvironmentVariable("GmailEmail");
                GmailPassword = Environment.GetEnvironmentVariable("GmailPassword");
                BotToken = Environment.GetEnvironmentVariable("BotToken");
                ClientID = Environment.GetEnvironmentVariable("ClientID");
                RefreshToken = Environment.GetEnvironmentVariable("RefreshToken");
                ChannelToken = Environment.GetEnvironmentVariable("ChannelToken");
                JsonBinSecret = Environment.GetEnvironmentVariable("JsonBinSecret");
                GoogleApiKey = Environment.GetEnvironmentVariable("GoogleApiKey");
            }
        }

        public static string ChannelName { get; private set; }
        public static string BotUsername { get; private set; }
        public static string BotPassword { get; private set; }
        public static string GmailEmail { get; private set; }
        public static string GmailPassword { get; private set; }
        public static string BotToken { get; private set; }
        public static string ChannelToken { get; private set; }
        public static string ClientID { get; private set; }
        public static string RefreshToken { get; private set; }
        public static string JsonBinSecret { get; private set; }
        public static string GoogleApiKey { get; private set; }
    }
}
