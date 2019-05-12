using System;
using System.IO;

namespace Winwink.MySqlite.REPL.User
{
    public class UserTable
    {
        public int MaxRowNumber = 0;

        public Pager Pager = new Pager();

        public static UserTable Open(string filePath)
        {
            var pager = Pager.PagerOpen(filePath);
            UserTable table = new UserTable();
            UserRow row = new UserRow();
            table.Pager = pager;
            table.MaxRowNumber = (int)pager.FileLength / row.RowSize;
            return table;
        }

        public void Close()
        {
            Save();
            Pager.Dispose();
        }

        public void Insert(UserRow row)
        {
            var tableMaxRows = Pager.TableMaxPages * row.RowPerPage;
            if (MaxRowNumber >= tableMaxRows)
            {
                Console.WriteLine("Table Full");
                return;
            }

            var bytes = row.Serialize();
            var pageNumber = MaxRowNumber / row.RowPerPage;
            var rowNumberInPage = MaxRowNumber % row.RowPerPage;
            var offset = rowNumberInPage * row.RowSize;
            var page = Pager.GetPage(pageNumber);

            Array.Copy(bytes, 0, page, offset, row.RowSize);
            MaxRowNumber++;
        }

        public UserRow[] Select()
        {
            UserRow userRow = new UserRow();
            UserRow[] result = new UserRow[MaxRowNumber];
            Console.WriteLine("Count:" + (MaxRowNumber));
            Console.WriteLine("id\tusername\temail");
            for (int i = 0; i < MaxRowNumber; i++)
            {
                var pageNumber = i / userRow.RowPerPage;
                var rowNumberInPage = i % userRow.RowPerPage;
                var offset = rowNumberInPage * userRow.RowSize;
                byte[] rowData = new byte[userRow.RowSize];

                Array.Copy(Pager.GetPage(pageNumber), offset, rowData, 0, userRow.RowSize);

                var row = UserRow.DeSerialize(rowData);

                Console.WriteLine($"{row.id}\t{row.username}\t{row.email}");
                result[i] = row;
            }
            return result;
        }

        /// <summary>
        /// 保存到磁盘
        /// </summary>
        public void Save()
        {
            UserRow userRow = new UserRow();
            var pageNumber = MaxRowNumber / userRow.RowPerPage;
            for (int i = 0; i < pageNumber; i++)
            {
                Pager.Flush(i, Pager.PageSize);
            }

            var additionRowsNumber = MaxRowNumber % userRow.RowPerPage;
            if (additionRowsNumber > 0)
            {
                Pager.Flush(pageNumber, additionRowsNumber * userRow.RowSize);
            }
        }
    }
}