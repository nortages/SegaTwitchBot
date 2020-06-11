using System;
using System.Threading;

namespace SegaTwitchBot
{
    class Program
    {
        static void Main(string[] args)
        {
            TwitchChatBot bot = new TwitchChatBot();
            bot.Connect();

            Thread.Sleep(-1);
        }        
    }    
}