﻿using System;
using System.IO;
using Winwink.MySqlite.REPL.User;

namespace Winwink.MySqlite.REPL
{
    class Program
    {
        static void Main(string[] args)
        {
            string filePath = Path.Combine(Environment.CurrentDirectory, "Mysqlite.data");
            UserTable table = UserTable.Open(filePath);
            CommaParser parser = new CommaParser();
            parser.Table = table;
            while (true)
            {
                var input = Console.ReadLine();
                parser.Parser(input);
            }
        }

        static void PrintPrompt()
        {
            Console.WriteLine("db > ");
        }
    }
}
