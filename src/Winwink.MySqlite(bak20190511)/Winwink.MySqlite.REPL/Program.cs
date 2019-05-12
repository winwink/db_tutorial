using System;

namespace Winwink.MySqlite.REPL
{
    class Program
    {
        static void Main(string[] args)
        {
            UserTable.Load();
            while(true)
            {
                var input = Console.ReadLine();
                CommaParser parser = new CommaParser();
                parser.Parser(input);
            }
        }

        static void PrintPrompt()
        {
            Console.WriteLine("db > ");
        }
    }
}
