using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Winwink.MySqlite.REPL.Test
{
    [TestClass]
    public class UserTableTest
    {
        [TestMethod]
        public void SerializeTest()
        {
            UserRow user = new UserRow();
            user.id = 1;
            user.username = "zhugaopan";
            user.email = "zhugp@jieysoft.com";
            var bytes = user.Serialize();
            var user2 = UserRow.DeSerialize(bytes);
            Assert.AreEqual(user2.id, user.id);
            Assert.AreEqual(user2.username, user.username);
            Assert.AreEqual(user2.email, user.email);
        }

        [TestMethod]
        public void InsertTest()
        {
            CommaParser parser = new CommaParser();
            for (int i = 0; i < 1400; i++)
            {
                parser.Parser($"insert {i} {i+"a"} {i+"b"}");
            }
            parser.Parser("save");
            parser.Parser("exit");
        }

    }
}
