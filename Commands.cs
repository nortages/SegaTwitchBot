using Google.Apis.Sheets.v4.Data;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using Newtonsoft.Json;
using System.Net.Http;
using System.Net;
using Google.Apis.Sheets.v4;

namespace NortagesTwitchBot
{
    public static class Commands
    {
        const string linkToHOF = "https://docs.google.com/spreadsheets/d/19RwGl1i79-3ZuVYyytfyvsg_wVprvozMSyooAy3HaU8";
        const string spreadsheetId_HOF = "19RwGl1i79-3ZuVYyytfyvsg_wVprvozMSyooAy3HaU8";
        static bool timeToPolling = false;
        static Dictionary<string, int> votes;
        public static TwitchClient client;
        public static ChromeDriver driver;
        public static SheetsService sheetsService;

        static Commands()
        {
            client = TwitchChatBot.client;
            sheetsService = TwitchChatBot.sheetsService;
        }

        public static void BansCommand(OnChatCommandReceivedArgs e)
        {
            string output;
            var senderUsername = e.Command.ChatMessage.DisplayName;
            try
            {
                if (string.IsNullOrEmpty(e.Command.ArgumentsAsString))
                {
                    (string timeouts, string bans) = GetChannelStats(senderUsername);
                    output = $"@{senderUsername}, you have got {timeouts} timeouts and {bans} bans.";
                }
                else
                {
                    var userName = e.Command.ArgumentsAsString.TrimStart('@');
                    (string timeouts, string bans) = GetChannelStats(userName);
                    output = $"@{senderUsername}, the user {userName} has {timeouts} timeouts and {bans} bans.";
                }
            }
            catch (NoSuchElementException)
            {
                output = $"@{senderUsername}, the user {e.Command.ArgumentsAsString} isn't present on the stream now.";
            }

            client.SendMessage(e.Command.ChatMessage.Channel, output);
            Console.WriteLine(output);
        }

        public static void SongCommand(OnChatCommandReceivedArgs e)
        {
            //var group = vk_api.Groups.GetByIdAsync(null, "120235040", GroupsFields.Status).Result.FirstOrDefault();
            //var result = group.StatusAudio != null ? $"{group.StatusAudio.Artist} - {group.StatusAudio.Title}" : "Сейчас у стримера в вк ничего не играет :(";
            //Console.WriteLine("Current song: " + result);
            //client.SendMessage(joinedChannel, result);
            var prefix = string.IsNullOrEmpty(e.Command.ArgumentsAsString) ? "" : e.Command.ArgumentsAsString + ", ";
            var message = "Все песни, кроме тех, что с ютуба, транслируются у стримера в группе вк, заходи GivePLZ https://vk.com/k_i_ra_group TakeNRG";
            client.SendMessage(e.Command.ChatMessage.Channel, $"{prefix}{message}");
        }

        #region Currently not used

        public static async Task ShowResult(OnChatCommandReceivedArgs e)
        {
            if (int.TryParse(e.Command.ArgumentsAsString, out int result))
            {
                Console.WriteLine($"The result is {result}");

                var bin_id = "5ede5401655d87580c463af7";
                var url = $"https://api.jsonbin.io/b/{bin_id}";

                var response = await new HttpClient().GetAsync(url);
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    var votes = JsonConvert.DeserializeObject<Dictionary<string, int>>(await response.Content.ReadAsStringAsync());
                    string winner = votes.OrderBy(item => Math.Abs(result - item.Value)).First().Key;
                    var message = $"PorscheWIN Победитель - {winner}. Его ставка - {votes[winner]}. Поздравляем! EZY Clap";
                    client.SendMessage(e.Command.ChatMessage.Channel, message);
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

        public static async Task StopVotingCommand(OnChatCommandReceivedArgs e)
        {
            if (timeToPolling) return;
            timeToPolling = false;
            var bin_id = "5ede5401655d87580c463af7";
            var url = $"https://api.jsonbin.io/b/{bin_id}";
            var content = new StringContent(
              JsonConvert.SerializeObject(votes),
              Encoding.UTF8,
              "application/json"
              );
            var response = await new HttpClient().PutAsync(url, content);

            if (response.StatusCode != HttpStatusCode.OK)
            {
                Console.WriteLine("Smth went wrong...");
                Console.WriteLine(response.StatusCode);
                Console.WriteLine(response.ReasonPhrase);
            }

            Console.WriteLine("Polling just closed!");
            var message = "Голосование закончилось! FBtouchdown Ожидайте конца стрима, чтобы узнать результат TwitchLit";
            client.SendMessage(e.Command.ChatMessage.Channel, message);
        }

        public static void StartVotingCommand(OnChatCommandReceivedArgs e)
        {
            timeToPolling = true;
            votes = new Dictionary<string, int>();
            client.SendMessage(e.Command.ChatMessage.Channel, "Голосование началось! TwitchVotes Пишите !ммр и свою ставку. Под конец стрима будет определён победитель, удачи! TakeNRG");
            Console.WriteLine("Polling just started!");
        }

        public static void HallOfFameCommand(OnChatCommandReceivedArgs e)
        {
            var winners = GetHallOfFame();
            if (e.Command.ArgumentsAsString == "фулл")
            {
                var msg = "Полный список лучших ванг стрима этого месяца Pog\n";
                client.SendMessage(e.Command.ChatMessage.Channel, msg + linkToHOF);
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
                client.SendMessage(e.Command.ChatMessage.Channel, msg);
            }
        }

        public static void MmrCommand(OnChatCommandReceivedArgs e)
        {
            if (timeToPolling && int.TryParse(e.Command.ArgumentsAsString, out int vote))
            {
                votes[e.Command.ChatMessage.DisplayName] = vote;
                Console.WriteLine($"{e.Command.ChatMessage.DisplayName} votes for {vote}");
            }
        }

        #endregion Currently not used

        private static (string timeouts, string bans) GetChannelStats(string userName)
        {
            var inputElement = driver.FindElement(By.XPath("//input[@name='viewers-filter']"), 10);
            inputElement.Clear();
            inputElement.SendKeys(userName);

            var userElement = driver.FindElement(By.XPath($"//p[text()='{userName.ToLower()}']"), 5);
            userElement.Click();
            var infoPanel = driver.FindElement(By.XPath("//div[@data-test-selector='viewer-card-mod-drawer']"), 2);
            var panelElements = infoPanel.FindElements(By.XPath(".//div[@data-test-selector='viewer-card-mod-drawer-tab']"));

            var xpath = ".//p[contains(@class, 'tw-c-text-link')]";
            var timeouts = panelElements[1].FindElement(By.XPath(xpath), 3).Text;
            var bans = panelElements[2].FindElement(By.XPath(xpath), 3).Text;

            driver.FindElement(By.XPath("//button[@data-a-target='user-details-close']")).Click();

            return (timeouts, bans);
        }

        static Dictionary<string, int> GetHallOfFame()
        {
            string rangeToRead = "HallOfFame!A2:B";
            var request = sheetsService.Spreadsheets.Values.Get(spreadsheetId_HOF, rangeToRead);

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

        static void UpdateHallOfFame(string winner)
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
            var upd_request = sheetsService.Spreadsheets.Values.Update(body, spreadsheetId_HOF, rangeToWrite);
            upd_request.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;
            upd_request.Execute();
        }
    }
}
