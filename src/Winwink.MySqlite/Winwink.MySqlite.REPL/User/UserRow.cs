using System;
using System.Collections.Generic;
using System.Text;

namespace Winwink.MySqlite.REPL.User
{
    public class UserRow
    {
        public int id;
        public string username;//varchar(32)
        public string email;//varchar(255)
        public int RowSize = 4 + 32 + 255;//单行大小
        public int RowPerPage {
            get { return Pager.PageSize / RowSize; } 
        } //单页最大行数 14
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
    }
}