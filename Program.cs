using System;
using System.Threading;

namespace NortagesTwitchBot
{
    class Program
    {
        static void Main(string[] args)
        {
            var bot = new TwitchChatBot();
            bot.Connect();

            Thread.Sleep(-1);
        }        
    }    
}