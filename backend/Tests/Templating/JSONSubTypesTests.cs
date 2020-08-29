//using JsonSubTypes;
//using Newtonsoft.Json;
//using NUnit.Framework;
//using System;
//using System.Collections.Generic;
//using System.Text;

//namespace Tests.Templating
//{
//    [Explicit]
//    [Ignore("Only manual")]
//    class JSONSubTypesTests
//    {
//        [JsonConverter(typeof(JsonSubtypes), "type")]
//        internal interface IAnimal
//        {
//            string type { get; }
//        }
//        internal class NaemonServiceTemplate : IAnimal
//        {
//            public string Description { get; set; }
//            public string type { get; } = "qwe";
//        }
//        internal class NaemonHostTemplates : IAnimal
//        {
//            public bool Declawed { get; set; }
//            public string type { get; } = "Host";
//        }

//        internal class Dog : IAnimal
//        {
//            public string Breed { get; set; }
//            public string type { get; } = "Dog";
//        }
//        internal class Cat : IAnimal
//        {
//            public bool Declawed { get; set; }
//            public string type { get; } = "Cat";
//        }
//        internal class Mouse : IAnimal
//        {
//            public bool Declawed { get; set; }
//            public string type { get; } = "Mouse";
//        }

//        [Test]
//        public static void TestBasics()
//        {
//            //var animal = JsonConvert.DeserializeObject<IAnimal[]>("[{\"type\":\"Dog\",\"Breed\":\"Jack Russell Terrier\"}]");
//            //Assert.AreEqual("Jack Russell Terrier", (animal[0] as Dog)?.Breed);

//            //var s = JsonConvert.DeserializeObject<INaemonFragmentTemplate[]>(@"[
//            //    {
//            //    ""type"": ""service"",
//            //    ""description"": ""20 generic application \""Application_0\"""",
//            //    ""command"": {
//            //                    ""executable"": ""check_application"",
//            //    ""parameters"": ""--application-name \""Application_0\""""
//            //    }
//            //            },{
//            //    ""type"": ""service"",
//            //    ""description"": ""20 generic application \""Application_1\"""",
//            //    ""command"": {
//            //                    ""executable"": ""check_application"",
//            //    ""parameters"": ""--application-name \""Application_1\""""
//            //    }
//            //            }
//            //]");
//            var s = JsonConvert.DeserializeObject<IAnimal[]>("[{\"type\":\"Host\",\"description\":\"20 generic application\"}]");
//            Console.WriteLine(s);

//        }
//    }
//}
