using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.IO;
using System.Text;

namespace Winwink.MySqlite.REPL
{
    public class CommaParser
    {
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
            }
            ExecuteStatement(statement);
        }

        public MetaCommandResult DoMetaCommand(string input)
        {
            if (input.Equals("exit", StringComparison.OrdinalIgnoreCase))
            {
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
                    UserTable.Insert(statement.Row);
                    break;
                case StatementType_T.STATEMENT_SELECT:
                    Console.WriteLine("This is where we would do an select");
                    UserTable.Select();
                    break;
                case StatementType_T.STATEMENT_SAVE:
                    Console.WriteLine("Save");
                    UserTable.Save();
                    break;
            }
        }

    }
    public class Statement
    {
        public StatementType_T StatementType { get; set; }
        public UserRow Row;
        public int RowToInsert;
    }

    public class UserTable
    {
        public const int TableMaxPages = 100; //单表最大页数 100
        public const int TableMaxRows = TableMaxPages * UserRow.RowPerPage;//单表最大行数 1400
        public const int PageSize = 4096; //页大小

        private static readonly byte[][] Pages = new byte[TableMaxPages][];
        public static int MaxRowNumber = 0;
        public static int MaxPage = 0;
        
        private static readonly string SavePath = Path.Combine(Environment.CurrentDirectory, "Mysqlite.data");

        public static void Insert(UserRow row)
        {
            if (MaxRowNumber >= TableMaxRows)
            {
                Console.WriteLine("Table Full");
                return;
            }

            var bytes = row.Serialize();
            var pageNumber = MaxRowNumber / UserRow.RowPerPage;
            var rowNumberInPage = MaxRowNumber % UserRow.RowPerPage;
            var offset = rowNumberInPage * UserRow.RowSize;
            if (Pages[pageNumber] == null)
            {
                Pages[pageNumber] = new byte[PageSize];
                MaxPage = pageNumber;
            }

            Array.Copy(bytes, 0, Pages[pageNumber], offset, UserRow.RowSize);
            MaxRowNumber++;
        }

        public static UserRow[] Select()
        {
            UserRow[] result = new UserRow[MaxRowNumber];
            Console.WriteLine("Count:" + (MaxRowNumber + 1));
            Console.WriteLine("id\tusername\temail");
            for (int i = 0; i < MaxRowNumber; i++)
            {
                var pageNumber = i / UserRow.RowPerPage;
                var rowNumberInPage = i % UserRow.RowPerPage;
                var offset = rowNumberInPage * UserRow.RowSize;
                byte[] rowData = new byte[UserRow.RowSize];
                Array.Copy(Pages[pageNumber], offset, rowData, 0, UserRow.RowSize);

                var row = UserRow.DeSerialize(rowData);

                Console.WriteLine($"{row.id}\t{row.username}\t{row.email}");
                result[i] = row;
            }
            return result;
        }

        /// <summary>
        /// 保存到磁盘
        /// </summary>
        public static void Save()
        {
            using FileStream fs = new FileStream(SavePath, FileMode.Create);
            using BinaryWriter writer = new BinaryWriter(fs);
            for (int i = 0; i <= MaxPage; i++)
            {
                writer.Write(Pages[i]);
            }
            writer.Close();
            fs.Close();
        }

        /// <summary>
        /// 从磁盘加载
        /// </summary>
        public static void Load()
        {
            if (!File.Exists(SavePath)) return;

            using FileStream fs = new FileStream(SavePath, FileMode.Open);
            var index = 0;
            while (index <TableMaxPages)
            {
                Pages[index] = new byte[PageSize];
                var read = fs.Read(Pages[index], 0, PageSize);
                if (read == 0)
                {
                    break;
                }

                index++;
            }

            MaxPage = index - 1;
            MaxRowNumber = MaxPage * UserRow.RowPerPage;
            for (int i = 0; i < UserRow.RowPerPage; i++)
            {
                if (Pages[MaxPage][i * UserRow.RowSize] == 0)
                {
                    break;
                }
                MaxRowNumber++;
            }
        }
    }

    public class UserRow
    {
        public int id;
        public string username;//varchar(32)
        public string email;//varchar(255)
        public const int RowSize = 4 + 32 + 255;//单行大小
        public const int RowPerPage = UserTable.PageSize / RowSize;//单页最大行数 14
        public byte[] Serialize()
        {
            var byte1 = BitConverter.GetBytes(id);
            var byte2 = Encoding.ASCII.GetBytes(username);
            var byte3 = Encoding.ASCII.GetBytes(email);
            var length = RowSize;
            var bytes = new byte[length];

            Array.Copy(byte1, 0, bytes, 0, 4);
            Array.Copy(byte2, 0, bytes, 4, byte2.Length);
            Array.Copy(byte3, 0, bytes, 36, byte3.Length);
            return bytes;
        }

        public static UserRow DeSerialize(byte[] bytes)
        {
            UserRow user = new UserRow();
            user.id = BitConverter.ToInt32(bytes, 0);
            user.username = Encoding.ASCII.GetString(bytes, 4, 32).Trim('\0');
            user.email = Encoding.ASCII.GetString(bytes, 36, 255).Trim('\0');
            return user;
        }

        public static List<UserRow> DeSerializeArray(byte[] bytes)
        {
            List<UserRow> result = new List<UserRow>();
            byte[] buffer = new byte[RowSize];
            int index = 0;
            for (int i = 0; i < RowPerPage; i++)
            {
                var b = bytes[i * RowSize];
                if (b == 0)
                {
                    break;
                }
            }

            return result;
        }
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
