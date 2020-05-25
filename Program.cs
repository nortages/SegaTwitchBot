using System;

namespace SegaTwitchBot
{
    class Program
    {
        static void Main(string[] args)
        {
            TwitchChatBot bot = new TwitchChatBot();
            bot.Connect();
            
            Console.ReadLine();
        }        
    }    
}