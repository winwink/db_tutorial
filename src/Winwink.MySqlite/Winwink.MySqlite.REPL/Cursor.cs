using System;
using System.Collections.Generic;
using System.Text;
using Winwink.MySqlite.REPL.User;

namespace Winwink.MySqlite.REPL
{
    public class Cursor
    {
        public UserTable Table;
        public int RowNumber;
        public bool IsEndOfTable;

        public static Cursor TableStart(UserTable table)
        {
            Cursor cursor = new Cursor();
            cursor.Table = table;
            cursor.RowNumber = 0;
            cursor.IsEndOfTable = table.MaxRowNumber == 0;
            return cursor;
        }

        public static Cursor TableEnd(UserTable table)
        {
            Cursor cursor = new Cursor();
            cursor.Table = table;
            cursor.RowNumber = table.MaxRowNumber;
            cursor.IsEndOfTable = true;
            return cursor;
        }
    }
}
