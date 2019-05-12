using System;
using System.ComponentModel.Design;
using Winwink.MySqlite.REPL.User;

namespace Winwink.MySqlite.REPL
{
    public class CommaParser
    {
        public UserTable Table;
        public void Parser(string input)
        {
            var mataCommandResult = DoMetaCommand(input);
            switch ((int)mataCommandResult)
            {
                case (int)MetaCommandResult.META_COMMAND_SUCCESS:
                    break;
                case (int)MetaCommandResult.META_COMMAND_UNRECOGNIZED_COMMAND:
                    Console.WriteLine($"Unrecognized command '{input}'");
                    return;
                    break;
            }
            PrepareResult prepareResult = PrepareCommand(input, out var statement);
            switch ((int)prepareResult)
            {
                case (int)PrepareResult.PREPARE_SUCCESS:
                    break;
                case (int)PrepareResult.PREPARE_UNRECOGNIZED_STATEMENT:
                    Console.WriteLine($"Unrecognized keyword at start '{input}'");
                    return;
                    break;
                case (int)PrepareResult.PREPARE_SYNTAX_ERROR:
                    Console.WriteLine($"Syntax error '{input}'");
                    return;
                    break;
            }
            ExecuteStatement(statement);
        }

        public MetaCommandResult DoMetaCommand(string input)
        {
            if (input.Equals("exit", StringComparison.OrdinalIgnoreCase))
            {
                Table.Close();
                Environment.Exit(0);
            }
            else
            {
                return MetaCommandResult.META_COMMAND_SUCCESS;
            }
            return MetaCommandResult.META_COMMAND_SUCCESS;
        }

        public PrepareResult PrepareCommand(string input, out Statement statement)
        {
            statement = new Statement();
            if (input.Substring(0, 4).Equals("Save", StringComparison.OrdinalIgnoreCase))
            {
                statement.StatementType = StatementType_T.STATEMENT_SAVE;
            }
            else if (input.Substring(0, 6).Equals("insert", StringComparison.OrdinalIgnoreCase))
            {
                statement.StatementType = StatementType_T.STATEMENT_INSERT;
                input = input.Trim();
                var list = input.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                if (list.Length != 4)
                {
                    return PrepareResult.PREPARE_SYNTAX_ERROR;
                }
                UserRow row = new UserRow();
                row.id = int.Parse(list[1]);
                row.username = list[2];
                row.email = list[3];
                statement.Row = row;
            }
            else if (input.Substring(0, 6).Equals("select", StringComparison.OrdinalIgnoreCase))
            {
                statement.StatementType = StatementType_T.STATEMENT_SELECT;
            }

            return PrepareResult.PREPARE_SUCCESS;
        }

        public void ExecuteStatement(Statement statement)
        {
            switch (statement.StatementType)
            {
                case StatementType_T.STATEMENT_INSERT:

                    Console.WriteLine($@"This is where we would do an insert, id:{statement.Row.id}, 
userName:{statement.Row.username}, email:{statement.Row.email}");
                    Table.Insert(statement.Row);
                    break;
                case StatementType_T.STATEMENT_SELECT:
                    Console.WriteLine("This is where we would do an select");
                    Table.Select();
                    break;
                case StatementType_T.STATEMENT_SAVE:
                    Console.WriteLine("Save");
                    Table.Save();
                    break;
            }
        }

    }
    public class Statement
    {
        public StatementType_T StatementType { get; set; }
        public UserRow Row;
    }

    public enum MetaCommandResult
    {
        META_COMMAND_SUCCESS,
        META_COMMAND_UNRECOGNIZED_COMMAND
    }

    public enum PrepareResult
    {
        PREPARE_SUCCESS,
        PREPARE_UNRECOGNIZED_STATEMENT,
        PREPARE_SYNTAX_ERROR
    }

    public enum StatementType_T
    {
        STATEMENT_INSERT,
        STATEMENT_SELECT,
        STATEMENT_SAVE
    }
}
