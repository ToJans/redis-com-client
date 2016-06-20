using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using redis_com_client;

namespace redis_com_client_test
{
    [TestClass]
    public class BasicTest
    {

        private CacheManager SUT;

        [TestInitialize]
        public void Initialize()
        {
            SUT = new CacheManager();
            SUT.Init("test1");
        }

        [TestCleanup]
        public void Cleanup()
        {
            SUT.RemoveAll();
        }

        [TestMethod]
        public void Add()
        {
            SUT.Add("key1", "abcde");

            Assert.AreEqual("abcde", SUT.Get("key1"));
        }

        [TestMethod]
        public void GetByObject()
        {
            SUT.Add("dk", "123");
            Assert.AreEqual("123", SUT["dk"]);
        }

        [TestMethod]
        public void SetByObject()
        {
            SUT["dk"] = "123";
            Assert.AreEqual("123", SUT["dk"]);
        }

        [TestMethod]
        public void Array()
        {
            var array = new object[,] { { 1, 2, 3 }, { "a", "b", "c" }, { "aa", "bb", "cc" } };

            SUT["arraytest"] = array;

            var x = (object[,])SUT["arraytest"];
            Assert.AreEqual(int.Parse("1"), x[0, 0]); //It guarantees int32 to VBScropt compability.
        }

        [TestMethod]
        public void ArraySpecialChar()
        {
            var specialChar = "#$%ˆ@";
            var array = new object[,] { { 1, 2, 3 }, { specialChar, "b", "c" } };

            SUT["arraytest"] = array;

            var x = (object[,])SUT["arraytest"];
            Assert.AreEqual(specialChar, x[1, 0]);
        }

        [TestMethod]
        public void ArrayHtml()
        {
            var html = "<script>alert();</script>";
            var array = new object[,] { { 1, 2, 3 }, { html, "b", "c" } };

            SUT["arraytest"] = array;

            var x = (object[,])SUT["arraytest"];
            Assert.AreEqual(html, x[1, 0]);
        }

        [TestMethod]
        public void RemoveAllFromThisKey()
        {
            var AnotherSUT = new CacheManager();
            AnotherSUT.Init("test2");
            AnotherSUT.Add("firstname", "22222");
            AnotherSUT.Add("lastname", "33333");

            SUT.Add("firstname", "firstname123");
            SUT.Add("lastname", "lastname123");
            SUT.RemoveAll();

            Assert.IsNull(SUT["firstname"]);
            Assert.IsNull(SUT["lastname"]);
            Assert.IsNotNull(AnotherSUT["firstname"]);
            Assert.IsNotNull(AnotherSUT["lastname"]);

            AnotherSUT.RemoveAll();
        }

        [TestMethod]
        public void Exists()
        {
            SUT.Add("exists", "12344");
            Assert.IsTrue(SUT.Exists("exists"));
        }

        [TestMethod]
        public void NotExists()
        {
            Assert.IsFalse(SUT.Exists("notexists"));
        }

        [TestMethod]
        public void Remove()
        {
            SUT.Add("onekey", "12344");
            SUT.Remove("onekey");
            Assert.IsFalse(SUT.Exists("onekey"));
        }

        [TestMethod]
        public void SetExpirationExistingKey()
        {
            SUT.Add("key4", "12344");
            Assert.IsTrue(SUT.Exists("key4"));
            SUT.SetExpiration("key4", TimeSpan.FromSeconds(1));
            Thread.Sleep(2000);
            Assert.IsNull(SUT["key4"]);
        }

        [TestMethod]
        public void AddNullValue()
        {
            SUT.Add("null", null);
            Assert.IsTrue(SUT.Exists("null"));
        }

        [TestMethod]
        public void ReplaceDbNull()
        {
            var array = new object[,] { { 10, 20 }, { "asdf", DBNull.Value } };
            SUT["DBNull"] = array;

            var result = (object[,])SUT["DBNull"];
            Assert.IsNotNull(result);
            Assert.IsNull(result[1, 1]);
        }

        [TestMethod]
        public void AddSameKeyTwice()
        {
            SUT["twice"] = 1;
            Thread.Sleep(500);
            SUT["twice"] = "asdf";
            Assert.AreEqual("asdf", SUT["twice"]);
        }

        [TestMethod]
        public void Concurrent()
        {
            ArrayHtml();
            var sb = new StringBuilder();
            Parallel.For((long)0, 1000, i =>
           {
               sb.AppendFormat("i: {0}{1}", i, Environment.NewLine);
               var sw = new Stopwatch();
               sw.Start();
               object x = SUT["arraytest"];
               sw.Stop();
               sb.AppendFormat("i: {0} - time: {1}ms", i, sw.ElapsedMilliseconds);
           });
            Console.WriteLine(sb);
        }

        [TestMethod]
        public void MyTableTwoDim()
        {
            var array = new object[,] { { 1, 2, 3 }, { "a", "b", "c" } };
            var table = new MyTable(array);

            var newArray = (object[,])table.GetArray();
            Assert.AreEqual(array[0, 0], newArray[0, 0]);
            Assert.AreEqual(array[1, 1], newArray[1, 1]);
        }

        [TestMethod]
        public void MyTableOneDim()
        {
            var array = new object[] { 1, 2, 3 };
            var table = new MyTable(array);

            var newArray = (object[])table.GetArray();
            Assert.AreEqual(array[0], newArray[0]);
            Assert.AreEqual(array[1], newArray[1]);
            Assert.AreEqual(array[2], newArray[2]);
        }

        [TestMethod]
        public void KeyExpiresAfterDefaultLifeTime()
        {
            var key = "abc";
            var value = "someval";

            SUT.DefaultLifeTime = TimeSpan.FromSeconds(1);

            SUT[key] = value;

            var cachedValue = SUT[key];
            Assert.AreEqual(value, cachedValue);

            Thread.Sleep(SUT.DefaultLifeTime + TimeSpan.FromSeconds(1));

            cachedValue = SUT[key];
            Assert.IsNull(cachedValue);
        }

        [TestMethod]
        public void KeyLifeTimeExtendsAfterGet()
        {
            var key = "abc";
            var value = "someval";
            var LifeTime = TimeSpan.FromSeconds(1);
            var SmallerThanLifeTime = LifeTime - TimeSpan.FromSeconds(1);
            var LargertThanLifeTime = LifeTime + TimeSpan.FromSeconds(1);

            SUT.DefaultLifeTime = LifeTime;

            SUT[key] = value;

            object cachedValue;

            // extend lifetime 3 times
            for (var i = 0; i < 3; i++)
            {
                Thread.Sleep(SmallerThanLifeTime);

                cachedValue = SUT[key];
                Assert.AreEqual(value, cachedValue);
            }

            Thread.Sleep(LargertThanLifeTime);

            cachedValue = SUT[key];
            Assert.IsNull(cachedValue);
        }
    }
}
