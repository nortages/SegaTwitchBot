using System;
using System.Collections.Generic;
using System.IO;
//using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace SegaTwitchBot
{
    static class TwitchInfo
    {
        //private static IConfiguration _configuration;

        static TwitchInfo()
        {
            //// This will get the current WORKING directory (i.e. \bin\Debug\netcoreapp)
            //string workingDirectory = Environment.CurrentDirectory;
            //// or: Directory.GetCurrentDirectory() gives the same result

            //// This will get the current PROJECT directory
            //string projectDirectory = Directory.GetParent(workingDirectory).Parent.Parent.FullName;

            //string pathToConfig = @$"{projectDirectory}\config.json";

            //if (File.Exists(pathToConfig))
            //{
            //    using StreamReader r = new StreamReader(pathToConfig);
            //    string json = r.ReadToEnd();
            //    var items = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);

            //    ChannelName = items["ChannelName"];
            //    BotUsername = items["BotUsername"];
            //    BotToken = items["BotToken"];
            //    ClientID = items["ClientID"];
            //    RefreshToken = items["RefreshToken"];
            //    JsonBinSecret = items["JsonBinSecret"];
            //}
            //else
            //{
            //    ChannelName = Environment.GetEnvironmentVariable("ChannelName");
            //    BotUsername = Environment.GetEnvironmentVariable("BotUsername");
            //    BotToken = Environment.GetEnvironmentVariable("BotToken");
            //    ClientID = Environment.GetEnvironmentVariable("ClientID");
            //    RefreshToken = Environment.GetEnvironmentVariable("RefreshToken");
            //    JsonBinSecret = Environment.GetEnvironmentVariable("JsonBinSecret");
            //}
            ChannelName = Environment.GetEnvironmentVariable("ChannelName");
            BotUsername = Environment.GetEnvironmentVariable("BotUsername");
            BotToken = Environment.GetEnvironmentVariable("BotToken");
            ClientID = Environment.GetEnvironmentVariable("ClientID");
            RefreshToken = Environment.GetEnvironmentVariable("RefreshToken");
            JsonBinSecret = Environment.GetEnvironmentVariable("JsonBinSecret");
        }

        public static string ChannelName { get; private set; }
        public static string BotUsername { get; private set; }
        public static string BotToken { get; private set; }
        public static string ClientID { get; private set; }
        public static string RefreshToken { get; private set; }
        public static string JsonBinSecret { get; private set; }
    }
}
