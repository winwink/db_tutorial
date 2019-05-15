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

        public bool IsEndOfTable => RowNumber == Table.MaxRowNumber;

        public static Cursor TableStart(UserTable table)
        {
            Cursor cursor = new Cursor();
            cursor.Table = table;
            cursor.RowNumber = 0;
            return cursor;
        }

        public static Cursor TableEnd(UserTable table)
        {
            Cursor cursor = new Cursor();
            cursor.Table = table;
            cursor.RowNumber = table.MaxRowNumber;
            return cursor;
        }

        public void CursorAdvance()
        {
            RowNumber++;
        }

        public void CursorValue(out byte[] page, out int offset)
        {
            UserRow userRow = new UserRow();
            var pageNumber = this.RowNumber / userRow.RowPerPage;
            var rowNumberInPage = this.RowNumber % userRow.RowPerPage;

            page = this.Table.Pager.GetPage(pageNumber);
            offset = rowNumberInPage * userRow.RowSize;
        }
    }
}
