using System;

using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Client.Extensions;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Clients;
using TwitchLib.Communication.Models;
using TwitchLib.PubSub.Events;
using TwitchLib.PubSub;

namespace SegaTwitchBot
{
    class TwitchChatBot
    {
        static TwitchClient client;
        static TwitchPubSub pubsub;
        static JoinedChannel joinedChannel;

        static bool isTimeOutBelow = false;

        public void Connect()
        {
            // JoinedChannel
            joinedChannel = new JoinedChannel(TwitchInfo.ChannelName);

            // TwitchClient
            ConnectionCredentials credentials = new ConnectionCredentials(TwitchInfo.BotUsername, TwitchInfo.BotToken);
            var clientOptions = new ClientOptions
            {
                MessagesAllowedInPeriod = 20,
                ThrottlingPeriod = TimeSpan.FromSeconds(30),
                SendDelay = 1
            };
            WebSocketClient customClient = new WebSocketClient(clientOptions);
            client = new TwitchClient(customClient);
            client.Initialize(credentials, TwitchInfo.ChannelName);

            client.OnLog += Client_OnLog;
            client.OnConnectionError += Client_OnConnectionError;
            client.OnJoinedChannel += Client_OnJoinedChannel;
            client.OnMessageReceived += Client_OnMessageReceived;
            client.OnConnected += Client_OnConnected;

            client.Connect();

            // PubSub
            pubsub = new TwitchPubSub();
            pubsub.OnPubSubServiceConnected += OnPubSubServiceConnected;
            pubsub.OnListenResponse += OnListenResponse;
            pubsub.OnRewardRedeemed += OnRewardRedeemed;

            pubsub.ListenToRewards(TwitchHelpers.GetUserId(TwitchInfo.ChannelName));

            pubsub.Connect();
        }

        // PUBSUB SUBSCRIBERS

        private static void OnRewardRedeemed(object sender, OnRewardRedeemedArgs e)
        {
            if (e.Status == "UNFULFILLED") {
                Console.WriteLine("\nSomeone redeemed a reward!");
                Console.WriteLine($"Name: {e.DisplayName},\nStatus: {e.Status},\nTitle: {e.RewardTitle},\nMessage: {e.Message},\nPrompt: {e.RewardPrompt}\n");

                if (e.RewardTitle.Contains("Таймач самому себе"))
                {
                    client.TimeoutUser(joinedChannel, e.DisplayName, TimeSpan.FromMinutes(10));
                }
                else if (e.RewardTitle.Contains("Таймач человеку снизу"))
                {
                    isTimeOutBelow = true;
                }
            }
        }

        private static void OnPubSubServiceConnected(object sender, EventArgs e)
        {
            // SendTopics accepts an oauth optionally, which is necessary for some topics
            Console.WriteLine("PubSub Service is Connected");

            pubsub.SendTopics(TwitchInfo.BotToken);
        }

        private static void OnListenResponse(object sender, OnListenResponseArgs e)
        {
            if (e.Successful)
                Console.WriteLine($"Successfully verified listening to topic: {e.Topic}");
            else
                Console.WriteLine($"Failed to listen! Error: {e.Response.Error}");
        }

        private void Client_OnConnectionError(object sender, OnConnectionErrorArgs e)
        {
            Console.WriteLine(e.Error);
        }

        // TWITCH CLIENT SUBSCRIBERS

        private void Client_OnLog(object sender, TwitchLib.Client.Events.OnLogArgs e)
        {
            Console.WriteLine($"{e.DateTime}: {e.BotUsername} - {e.Data}");
        }

        private void Client_OnConnected(object sender, OnConnectedArgs e)
        {
            Console.WriteLine($"Connected to {e.AutoJoinChannel}");
        }

        private void Client_OnJoinedChannel(object sender, OnJoinedChannelArgs e)
        {
            Console.WriteLine("Hey guys! I am a bot connected via TwitchLib!");
        }

        private void Client_OnMessageReceived(object sender, OnMessageReceivedArgs e)
        {
            if (isTimeOutBelow && !e.ChatMessage.IsModerator && !e.ChatMessage.IsBroadcaster)
            {
                client.TimeoutUser(joinedChannel, e.ChatMessage.DisplayName, TimeSpan.FromMinutes(10));
                isTimeOutBelow = false;
                Console.WriteLine($"{e.ChatMessage.DisplayName} is banned on 10 minutes!");
            }
        }

        internal void Disconnect()
        {
            Console.WriteLine("Disconnecting...");
            client.SendMessage(joinedChannel, "The plug has been pulled. My time is up. Until next time. ResidentSleeper");

            client.OnConnectionError -= Client_OnConnectionError; // will complain of a fatal network error if not disconnected. Is this something to fix?
            client.LeaveChannel(joinedChannel.Channel);

            pubsub.Disconnect();
            client.Disconnect();
        }
    }
}
