using System;
using System.IO;
using System.Net;
using System.Text;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text.RegularExpressions;

using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Client.Extensions;
using TwitchLib.Client.Models;
using TwitchLib.PubSub;
using TwitchLib.PubSub.Events;
using TwitchLib.Communication.Clients;
using TwitchLib.Communication.Models;

using Newtonsoft.Json;

using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Util.Store;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Sheets.v4.Data;

namespace SegaTwitchBot
{
    class TwitchChatBot
    {
        static TwitchClient client;
        static TwitchPubSub pubsub;
        static JoinedChannel joinedChannel;

        private static readonly HttpClient HTTPClient = new HttpClient();

        // If modifying these scopes, delete your previously saved credentials
        // at ~/.credentials/sheets.googleapis.com-dotnet-quickstart.json
        static readonly string[] Scopes = { SheetsService.Scope.Spreadsheets };
        static readonly string ApplicationName = "Google Sheets API .NET Quickstart";
        static SheetsService service;
        const string linkToHOF = "https://docs.google.com/spreadsheets/d/19RwGl1i79-3ZuVYyytfyvsg_wVprvozMSyooAy3HaU8";
        const string spreadsheetId = "19RwGl1i79-3ZuVYyytfyvsg_wVprvozMSyooAy3HaU8";

        const int TIMEOUTTIME = 10;
        static bool toTimeoutUserBelow = false;
        static Dictionary<string, int> votes;
        static readonly HashSet<string> usersWithShield = new HashSet<string>();
        static readonly Regex regex_botsPlusToChat = new Regex(@"[\wА-Яа-я]*[Бб]оты?,? \+ в чат[\wА-Яа-я]*");
        static bool timeToPolling = false;

        public void Connect()
        {
            HTTPClient.DefaultRequestHeaders.Add("secret-key", TwitchInfo.JsonBinSecret);
            HTTPClient.DefaultRequestHeaders.Add("versioning", "false");

            // JoinedChannel
            joinedChannel = new JoinedChannel(TwitchInfo.ChannelName);

            // TwitchClient
            ConnectionCredentials credentials = new ConnectionCredentials(TwitchInfo.BotUsername, TwitchInfo.BotToken);
            var clientOptions = new ClientOptions
            {
                MessagesAllowedInPeriod = 100,
                ThrottlingPeriod = TimeSpan.FromSeconds(30),
                SendDelay = 1
            };
            WebSocketClient customClient = new WebSocketClient(clientOptions);
            client = new TwitchClient(customClient);
            client.Initialize(credentials, TwitchInfo.ChannelName);

            //client.OnLog += Client_OnLog;
            client.OnConnectionError += Client_OnConnectionError;
            client.OnJoinedChannel += Client_OnJoinedChannel;
            client.OnMessageReceived += Client_OnMessageReceivedAsync;
            client.OnConnected += Client_OnConnected;

            client.Connect();

            // PubSub
            pubsub = new TwitchPubSub();
            pubsub.OnPubSubServiceConnected += OnPubSubServiceConnected;
            pubsub.OnListenResponse += OnListenResponse;
            pubsub.OnRewardRedeemed += OnRewardRedeemed;

            pubsub.ListenToRewards(TwitchHelpers.GetUserId(TwitchInfo.ChannelName));

            pubsub.Connect();

            // GoogleSheets
            //UserCredential credential;

            //using (var stream = new FileStream("credentials.json", FileMode.Open, FileAccess.Read))
            //{
            //    // The file token.json stores the user's access and refresh tokens, and is created
            //    // automatically when the authorization flow completes for the first time.
            //    string credPath = "token.json";
            //    credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
            //        GoogleClientSecrets.Load(stream).Secrets,
            //        Scopes,
            //        "user",
            //        CancellationToken.None,
            //        new FileDataStore(credPath, true)).Result;
            //    Console.WriteLine("Credential file saved to: " + credPath);
            //}

            // Create Google Sheets API service.
            service = new SheetsService(new BaseClientService.Initializer()
            {
                ApiKey = TwitchInfo.GoogleApiKey,
                //HttpClientInitializer = credential,
                ApplicationName = ApplicationName,
            });
        }

        // PUBSUB SUBSCRIBERS

        private static void OnRewardRedeemed(object sender, OnRewardRedeemedArgs e)
        {
            if (e.Status == "UNFULFILLED") {
                Console.WriteLine("\nSomeone redeemed a reward!");
                Console.WriteLine($"Name: {e.DisplayName},\nStatus: {e.Status},\nTitle: {e.RewardTitle},\nMessage: {e.Message},\nPrompt: {e.RewardPrompt}\n");

                var rewardTestName = "Сдать дань"; // TODO: Change rewardTestName

                if (e.RewardTitle.Contains("Таймач самому себе"))
                {
                    client.TimeoutUser(joinedChannel, e.DisplayName, TimeSpan.FromMinutes(10));
                }
                else if (e.RewardTitle.Contains("Таймач человеку снизу"))
                {
                    toTimeoutUserBelow = true;
                }
                else if (false && e.RewardTitle.Contains(rewardTestName))
                {
                    Console.WriteLine($"{e.DisplayName} took a shield!");
                    usersWithShield.Add(e.DisplayName);
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
            client.SendWhisper("segatron99k", "loh");
        }

        private void Client_OnConnectionError(object sender, OnConnectionErrorArgs e)
        {
            Console.WriteLine(e.Error);
        }

        // TWITCH CLIENT SUBSCRIBERS

        private void Client_OnLog(object sender, TwitchLib.Client.Events.OnLogArgs e)
        {
            Console.WriteLine($"{e.DateTime}: {e.BotUsername} - {e.Data}");
            // TODO: Once in a while save logs to a file
        }

        private void Client_OnConnected(object sender, OnConnectedArgs e)
        {
            Console.WriteLine($"Connected to {e.AutoJoinChannel}");
        }

        private void Client_OnJoinedChannel(object sender, OnJoinedChannelArgs e)
        {
            Console.WriteLine("Hey guys! I am a bot connected via TwitchLib!");
        }

        private async void Client_OnMessageReceivedAsync(object sender, OnMessageReceivedArgs e)
        {
            if (toTimeoutUserBelow && !e.ChatMessage.IsModerator && !e.ChatMessage.IsBroadcaster)
            {
                if (usersWithShield.Contains(e.ChatMessage.DisplayName))
                {
                    Console.WriteLine($"{e.ChatMessage.DisplayName} lose a shield!");
                    client.SendMessage(joinedChannel, $"@{e.ChatMessage.DisplayName}, Твой бабл лопнул CurseLit Теперь будь осторожнее Keepo");
                    usersWithShield.Remove(e.ChatMessage.DisplayName);
                    return;
                }
                client.TimeoutUser(joinedChannel, e.ChatMessage.DisplayName, TimeSpan.FromMinutes(TIMEOUTTIME));
                if (e.ChatMessage.DisplayName.ToLower() == "segatron99k")
                {
                    client.SendMessage(joinedChannel, "Прости, создатель PepeHands");
                }
                toTimeoutUserBelow = false;
                Console.WriteLine($"{e.ChatMessage.DisplayName} is banned on {TIMEOUTTIME} minutes!");
                return;
            }
            if (regex_botsPlusToChat.Matches(e.ChatMessage.Message).Count > 0)
            {
                client.SendMessage(joinedChannel, "+");
            }
            else if (e.ChatMessage.Message.StartsWith('!'))
            {
                if (timeToPolling && e.ChatMessage.Message.Contains("ммр"))
                {
                    if (int.TryParse(e.ChatMessage.Message.Split(' ').Last(), out int vote))
                    {
                        votes[e.ChatMessage.DisplayName] = vote;
                        Console.WriteLine($"{e.ChatMessage.DisplayName} votes for {vote}");
                    }
                }
                else if (e.ChatMessage.Message.Contains("залславы"))
                {
                    var winners = GetHallOfFame();
                    if (e.ChatMessage.Message.Contains("фулл"))
                    {
                        var msg = "Полный список лучших ванг стрима этого месяца Pog\n";
                        client.SendMessage(joinedChannel, msg + linkToHOF);
                    }
                    else
                    {
                        var msg = "Топ-3 ванг стрима Pog\n";
                        int i = 0;
                        foreach (var winner in winners)
                        {
                            msg += $"{winner.Key} - {winner.Value}; ";
                            if (i == 3) break;
                            i++;
                        }
                        msg = msg.Remove(msg.Length - 2, 2) + ".";
                        client.SendMessage(joinedChannel, msg);
                    }
                }
                else if (e.ChatMessage.Message.Contains("начатьголосование") && (e.ChatMessage.IsModerator || e.ChatMessage.IsBroadcaster))
                {
                    timeToPolling = true;
                    votes = new Dictionary<string, int>();
                    client.SendMessage(joinedChannel, "Голосование началось! TwitchVotes Пишите !ммр и свою ставку. Под конец стрима будет определён победитель, удачи! TakeNRG");
                    Console.WriteLine("Polling just started!");
                }
                else if (timeToPolling && e.ChatMessage.Message.Contains("закончитьголосование") && (e.ChatMessage.IsModerator || e.ChatMessage.IsBroadcaster))
                {
                    timeToPolling = false;
                    var bin_id = "5ede5401655d87580c463af7";
                    var url = $"https://api.jsonbin.io/b/{bin_id}";
                    var content = new StringContent(
                      JsonConvert.SerializeObject(votes),
                      Encoding.UTF8, 
                      "application/json"
                      );
                    var response = await HTTPClient.PutAsync(url, content);

                    if (response.StatusCode != HttpStatusCode.OK)
                    {
                        Console.WriteLine("Smth went wrong...");
                        Console.WriteLine(response.StatusCode);
                        Console.WriteLine(response.ReasonPhrase);
                    }

                    Console.WriteLine("Polling just closed!");
                    client.SendMessage(joinedChannel, "Голосование закончилось! FBtouchdown Ожидайте конца стрима, чтобы узнать результат TwitchLit");
                }
                else if (e.ChatMessage.Message.Contains("показатьрезультат") && (e.ChatMessage.IsModerator || e.ChatMessage.IsBroadcaster))
                {
                    if (int.TryParse(e.ChatMessage.Message.Split(' ').Last(), out int result))
                    {
                        Console.WriteLine($"The result is {result}");

                        var bin_id = "5ede5401655d87580c463af7";
                        var url = $"https://api.jsonbin.io/b/{bin_id}";

                        var response = await HTTPClient.GetAsync(url);
                        if (response.StatusCode == HttpStatusCode.OK)
                        {
                            var votes = JsonConvert.DeserializeObject<Dictionary<string, int>>(await response.Content.ReadAsStringAsync());
                            string winner = votes.OrderBy(item => Math.Abs(result - item.Value)).First().Key;
                            client.SendMessage(joinedChannel, $"PorscheWIN Победитель - {winner}. Его ставка - {votes[winner]}. Поздравляем! EZY Clap");
                            UpdateHallOfFame(winner);
                            Console.WriteLine($"The winner is {winner}! His bet was {votes[winner]}");
                        }
                        else
                        {
                            Console.WriteLine("Smth went wrong...");
                            Console.WriteLine(response.ReasonPhrase);
                        }                        
                    }
                }
            }
        }

        Dictionary<string, int> GetHallOfFame()
        {
            string rangeToRead = "HallOfFame!A2:B";
            var request = service.Spreadsheets.Values.Get(spreadsheetId, rangeToRead);

            ValueRange response = request.Execute();
            var values = response.Values;

            if (values != null && values.Count > 0)
            {
                Dictionary<string, int> d = new Dictionary<string, int>();
                foreach (var row in values)
                {
                    d.Add(row[0].ToString(), Convert.ToInt32(row[1]));
                }
                return d;
            }
            else
            {
                Console.WriteLine("No data found.");
                return new Dictionary<string, int>();
            }
        }

        void UpdateHallOfFame(string winner)
        {
            string rangeToWrite = "HallOfFame!A2";

            var d = GetHallOfFame();

            var key_name = winner;
            if (!d.ContainsKey(key_name))
            {
                d.Add(key_name, 0);
            }
            d[key_name]++;

            var upd_values = new List<IList<object>>();

            foreach (var item in d.OrderByDescending(key => key.Value))
            {
                upd_values.Add(new List<object> { item.Key, item.Value });
            }

            ValueRange body = new ValueRange { Values = upd_values };
            var upd_request = service.Spreadsheets.Values.Update(body, spreadsheetId, rangeToWrite);
            upd_request.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;
            upd_request.Execute();

        }

        internal void Disconnect()
        {
            Console.WriteLine("Disconnecting...");
            //client.SendMessage(joinedChannel, "The plug has been pulled. My time is up. Until next time. ResidentSleeper");

            client.OnConnectionError -= Client_OnConnectionError; // will complain of a fatal network error if not disconnected. Is this something to fix?
            client.LeaveChannel(joinedChannel.Channel);

            pubsub.Disconnect();
            client.Disconnect();
        }
    }
}
