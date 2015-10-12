using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace mg_console
{
    class Program
    {
        static void Main(string[] args)
        {
            //这是一个公共的调试默认连接
            var client = new MongoClient("mongodb://localhost");
            var server = client.GetServer();
            var database = server.GetDatabase("foo");

            //如果要连接多个服务器，可以直接填写多个服务器名（以及需要的端口号），并且以‘，’分割。如下：
            //mongodb://server1,server2:27017,server2:27018

            //多数据库服务是模糊不清的，不能分辨服务是否复本集，或者是多数据库服务。drive驱动会跳过connection string的语法检查，
            //直接连接进数据库服务器，让server自己检查他们的类别。还有一些办法在连接的时候就指定数据服务器的类别，就是在connection string里面直接描述。如下：
            //mongodb://server1,server2:27017,server2:27018/?connect=replicaset

            //可用的连接模式包括：automatic (默认), direct, replica set, 以及shardrouter。
            //mongodb://server2/?ssl=true(安全链接)
            //在默认的情况下，server是通过本地的受信任的证书机构获取许可。在一些测试环境下面，测试server没有签署证书，为了缓解这个情况，
            //可以使用在connection string里面添加“sslverifycertificate=false”来屏蔽所有certificate errors（认证错误）。

            //Authentication
            //MongoDB支持两种认证方式。一种是在程序执行时，调用特定的方法。在执行特定的方法时，认证将会被使用。另外一种健壮的方法是在MongoCredentialsStore存储认证
            //使用credential store来确定admin和“foo”数据库的认证信息。除了使用“admin”以及“foo”连接入数据库，还可以使用默认的认证“test”。

            //------------------------------------------------------------------------------
            var url = new MongoUrl("mongodb://test:user@localhost:27017");
            var settings = MongoClientSettings.FromUrl(url);

            var adminCredentials = new MongoCredentials("admin", "user", true);
            var fooCredentials = new MongoCredentials("foo", "user", false);

            settings.CredentialsStore.AddCredentials("admin", adminCredentials);
            settings.CredentialsStore.AddCredentials("foo", fooCredentials);

            var client_1 = new MongoClient(settings);
            //------------------------------------------------------------------------------
            MongoClient client_2 = new MongoClient(); // connect to localhost
            MongoServer server_1 = client_2.GetServer();

            MongoDatabase test = server_1.GetDatabase("test");

            MongoCredentials credentials = new MongoCredentials("username", "password");
            MongoDatabase salaries = server_1.GetDatabase("salaries", credentials);
            //------------------------------------------------------------------------------
            //大多数的数据库设置从server对象中继承过来，并且提供了GetDatabase的重载。要override其他设置，可以调用CreateDataBaseSetting，在调用GetDataBase之前，改变设置。比如下面这样：
            var databaseSettings = server_1.CreateDatabaseSettings("test");
            databaseSettings.SlaveOk = true;
            var database_1 = server_1.GetDatabase(databaseSettings);
            //大多数的collection设置都是继承了collection对象，并且提供了GetCollection的多态性来方便你来重写一些常用的使用设置。
            //要重写其他的设置，先调用CreateCollectionSetting来改变设置，然后再调用GetCollection方法。比如下面代码：
            //GetCollection维持一个表的实例，如果你再次调用这个GetCollection，它会返回一样的内容。
            var collectionSettings = database_1.CreateCollectionSettings<BsonDocument>("test");
            collectionSettings.SlaveOk = true;
            var collection = database_1.GetCollection(collectionSettings);
            //插入函数。插入的对象可以是BsonDocument的实例对象，也可以是任何成功转换成BSON文档的类实例。例如：
            MongoCollection<Person> persons_object = database.GetCollection<Person>("Person");

            Random r = new Random(BitConverter.ToInt32(Guid.NewGuid().ToByteArray(), 0));
            int ages = r.Next(1,100);
            string name =  CreateSimplifiedChinese(5);
            Person p_entity = new Person()
            {
                name = name,
                age = ages,
                sex = ages % 2 == 0 ? true : false
            };
            persons_object.Insert(p_entity);//或者
            //persons_object.Insert<Person>(p_entity);
            //如果你要插入多重文档，InsertBatch要比Insert有效。

            //FindOne以及FindOneAs方法
            //要从collection中检索文档，可以使用这个方法。FindOne是最简单的。它会返回结果的第一个文档。例如：
            Person p_1 = persons_object.FindOne();
            //如果你想检索一个文档，但是它不是<TDefaultDocument>类型的，你需要用到FindOneAs方法。它允许你返回你需要的文档类型。例如：
            BsonDocument bson_1 = persons_object.FindOneAs<BsonDocument>();

            //Find和FindAs方法是用query语句来告诉服务器返回什么的文档。这个query（查询语句）的类型是IMongoQuery。IMongoQuery是一个标记接口，
            //被类识别后可以用来作为查询语言。最常见的方法是，我们可以使用Query创建类或者是QueryDocument类来创建query语句。
            //另外如果使用QueryWrapper封装任何类型query语句，query都可以被转变成BSON文档类型。
            //使用QueryDocument
            var query_1 = new QueryDocument("name", name);
            foreach (Person item in persons_object.Find(query_1))
            {
                Console.WriteLine("QueryDocument:" + item.name);
            }
            //使用Query Builder
            var query_2 = Query.EQ("name",name);
            foreach (Person item in persons_object.Find(query_2))
            {
                Console.WriteLine("Query.EQ:" + item.name);
            }
            //使用FindAs来获取非默认类型的返回文档
            var query_3 = Query<Person>.EQ(x => x.name, name);
            foreach (Person item in persons_object.Find(query_3))
            {
                Console.WriteLine("Query<Person>.EQ:" + item.name);
            }

            //Save<TDocument>方法
            //Save方法是Insert和Update的组合。如果文档的属性是有值的，它会成为Update，来对文档更新。否则将会创建一个新文档调用Insert方法。
            //修改性别
            var query_4 = Query.And(
                Query.EQ("name", "你这个人真是无聊到头顶了")
                //,Query.EQ("age",65)
            );

            Person person_bs1 = persons_object.FindOne(query_4);
            if (person_bs1!=null)
            {
                person_bs1.age = r.Next(1,200);
                persons_object.Save(person_bs1);
            }
            //TDocument必须要有个ID元素，否则你将调用Insert，将文档插入。

            //Update方法
            var query_5 = Query.And(
                Query.EQ("name", "你这个人真是无聊到头顶了")
                );

            var update_1 = new UpdateDocument { 
                {"$set",new BsonDocument("age",r.Next(1,200))}
            };

            WriteConcernResult updateP_1 = persons_object.Update(query_5, update_1);

            //FindAndModify方法
            //使用FindAndModify方法,你可以在一个原子操作里面查找一个匹配的文档并且修改更新.
            //FindAndModify通常用于单个的文档，如果匹配了多个文档，可以使用标准的排序方法匹配到你自己想要修改的文档。
            var query_6 = Query.And(Query.EQ("name", "你这个人真是无聊到头顶了"),Query.EQ("sex",true));
            var sortby_1 = SortBy.Descending("age");
            var update_3 = Update.Set("age", 112).Set("sex", false);
            var results_1 = persons_object.FindAndModify(query_6, sortby_1, update_3, true);
            var document_1 = results_1.ModifiedDocument;

            //MapReduce方法
            //Map/Reduce是一种从collection中聚合数据的方法。每个文档（或者使用选择query语句产生的是子集）被发送至map函数，
            //map函数会产生一个中间的值。这个中间的值会传送至reduce函数进行数据的聚合。 
            //下面的例子采集自MongoDB：The Definitive Guide（MongDB权威解析）的87页。它计算在collection中找到的每个key要被计算多少次
            //LT=>Less Than
            //GT=>More Than
            //EQ=>Equels
            //IN 
            var query = Query.And(Query.LT("age", 100), Query.GT("age", 90));
            var cursor = persons_object.Find(query);
            var firstBook = cursor.FirstOrDefault();
            var lastBook = cursor.LastOrDefault();

            //Query.All("name", "a", "b");//通过多个元素来匹配数组

            //Query.And(Query.EQ("name", "a"), Query.EQ("title", "t"));//同时满足多个条件

            //Query.EQ("name", "a");//等于

            //Query.Exists("type", true);//判断键值是否存在

            //Query.GT("value", 2);//大于>

            //Query.GTE("value", 3);//大于等于>=

            //Query.In("name", "a", "b");//包括指定的所有值,可以指定不同类型的条件和值

            //Query.LT("value", 9);//小于<

            //Query.LTE("value", 8);//小于等于<=

            //Query.Mod("value", 3, 1);//将查询值除以第一个给定值,若余数等于第二个给定值则返回该结果

            //Query.NE("name", "c");//不等于

            //Query.Nor(Array);//不包括数组中的值

            //Query.Not("name");//元素条件语句

            //Query.NotIn("name", "a", 2);//返回与数组中所有条件都不匹配的文档

            //Query.Or(Query.EQ("name", "a"), Query.EQ("title", "t"));//满足其中一个条件

            //Query.Size("name", 2);//给定键的长度

            //Query.Type("_id", BsonType.ObjectId);//给定键的类型

            //Query.Where(BsonJavaScript);//执行JavaScript

            //Query.Matches("Title", str);//模糊查询 相当于sql中like  -- str可包含正则表达式

            
            Console.ReadKey();
        }



        public class Person 
        {
            public ObjectId _id { get; set; }
            public string name { get; set; }
            public double age { get; set; }
            public bool sex { get; set; }

        }


        public static string CreateSimplifiedChinese(int strlength)
        {
            //定义一个字符串数组储存汉字编码的组成元素 
            string[] r = new String[16] { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "a", "b", "c", "d", "e", "f" };

            Random rnd = new Random(BitConverter.ToInt32(Guid.NewGuid().ToByteArray(), 0));

            //定义一个object数组用来 
            object[] bytes = new object[strlength];

            //每循环一次产生一个含两个元素的十六进制字节数组，并将其放入bject数组中 
            //每个汉字有四个区位码组成 
            //区位码第1位和区位码第2位作为字节数组第一个元素 
            //区位码第3位和区位码第4位作为字节数组第二个元素 

            for (int i = 0; i < strlength; i++)
            {
                //区位码第1位 
                int r1 = rnd.Next(11, 14);
                string str_r1 = r[r1].Trim();

                //区位码第2位 
                rnd = new Random(BitConverter.ToInt32(Guid.NewGuid().ToByteArray(), 0));//更换随机数发生器的种子避免产生重复值 
                int r2;
                if (r1 == 13)
                    r2 = rnd.Next(0, 7);
                else
                    r2 = rnd.Next(0, 16);
                string str_r2 = r[r2].Trim();

                //区位码第3位 
                rnd = new Random(BitConverter.ToInt32(Guid.NewGuid().ToByteArray(), 0));
                int r3 = rnd.Next(10, 16);
                string str_r3 = r[r3].Trim();

                //区位码第4位 
                rnd = new Random(BitConverter.ToInt32(Guid.NewGuid().ToByteArray(), 0));
                int r4;
                if (r3 == 10)
                {
                    r4 = rnd.Next(1, 16);
                }
                else if (r3 == 15)
                {
                    r4 = rnd.Next(0, 15);
                }
                else
                {
                    r4 = rnd.Next(0, 16);
                }
                string str_r4 = r[r4].Trim();

                //定义两个字节变量存储产生的随机汉字区位码 
                byte byte1 = Convert.ToByte(str_r1 + str_r2, 16);
                byte byte2 = Convert.ToByte(str_r3 + str_r4, 16);

                //将两个字节变量存储在字节数组中 
                byte[] str_r = new byte[] { byte1, byte2 };

                //将产生的一个汉字的字节数组放入object数组中
                bytes.SetValue(str_r, i);
            }


            //获取GB2312编码页（表） 
            Encoding gb = Encoding.GetEncoding("gb2312");
            Encoding utf8s = Encoding.GetEncoding("utf-8");

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < strlength; i++)
            {
                sb.Append(gb.GetString((byte[])Convert.ChangeType(bytes[i], typeof(byte[]))));
            }

            //根据汉字编码的字节数组解码出中文汉字 
            //string str1 = gb.GetString((byte[])Convert.ChangeType(bytes[0], typeof(byte[])));
            //string str2 = gb.GetString((byte[])Convert.ChangeType(bytes[1], typeof(byte[])));
            //string str3 = gb.GetString((byte[])Convert.ChangeType(bytes[2], typeof(byte[])));
            //string str4 = gb.GetString((byte[])Convert.ChangeType(bytes[3], typeof(byte[])));
            //string txt = str1 + str2 + str3 + str4;

            //Encoding.UTF8.GetString(Encoding.Convert(Encoding.GetEncodings("gb2312"), Encoding.UTF8, Encoding.GetEncodings("gb2312").get));

            System.Text.Encoding utf8, gb2312;
            //gb2312   
            gb2312 = System.Text.Encoding.GetEncoding("gb2312");
            //utf8   
            utf8 = System.Text.Encoding.GetEncoding("utf-8");
            byte[] gbs;
            gbs = gb2312.GetBytes(sb.ToString());
            gbs = System.Text.Encoding.Convert(gb2312, utf8, gbs);
            //返回转换后的字符   
            return utf8.GetString(gbs);

        }
    }
}
