using System;
using System.Threading;

namespace NortagesTwitchBot
{
    class Program
    {
        static void Main(string[] args)
        {
            TwitchChatBot.Connect();

            Thread.Sleep(-1);
        }        
    }    
}