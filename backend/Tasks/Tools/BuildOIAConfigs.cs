//using NUnit.Framework;
//using Omnikeeper.Base.Inbound;
//using System;

//namespace Tasks.Tools
//{
//    [Explicit]
//    class BuildOIAConfigs
//    {
//        [Test]
//        public void BuildOmnikeeperOIAConfig()
//        {
//            var config = new OKPluginOIAOmnikeeper.OnlineInboundAdapter.Config(
//                "https://example.com/backend/api",
//                "https://example.com/",
//                "maxbytes",
//                "landscape-omnikeeper-api",
//                "9c98eb5f-22f0-47d1-8123-810d7d96104f",
//                new string[] { "CMDB" },
//                new TimeSpan(0, 1, 0),
//                "omnikeeper_maxbytes"
//                );
//            var json = IOnlineInboundAdapter.IConfig.Serializer.SerializeToString(config);

//            Console.WriteLine(json);
//        }

//        [Test]
//        public void BuildSharepointOIAConfig()
//        {
//            var ExternalSharepointTenantID = new Guid("98061435-3c72-44d1-b37a-057e21f42801");
//            var ExternalSharepointClientID = new Guid("3d6e9642-5430-438c-b435-34d35b323b3a");
//            var ExternalSharepointClientSecret = "/w5tskeWck6EV2sx6Mue1tkL+dSw42VdMHlPS5plohw=";
//            var ExternalSharepointListID = new Guid("37800a8f-0107-445b-b70b-c783ba5a5ce3");

//            var config = new OKPluginOIASharepoint.Config(ExternalSharepointTenantID, "example.sharepoint.com", "play2", ExternalSharepointClientID,
//                ExternalSharepointClientSecret, true, new TimeSpan(0, 1, 0), "sharepoint",
//                new OKPluginOIASharepoint.Config.ListConfig[] { new OKPluginOIASharepoint.Config.ListConfig(ExternalSharepointListID,
//                    new OKPluginOIASharepoint.Config.ListColumnConfig[] {
//                        new OKPluginOIASharepoint.Config.ListColumnConfig("MobilePhone", "user.mobile_phone"),
//                        new OKPluginOIASharepoint.Config.ListColumnConfig("Company", "user.company"),
//                        new OKPluginOIASharepoint.Config.ListColumnConfig("GivenName", "user.first_name"),
//                        new OKPluginOIASharepoint.Config.ListColumnConfig("Surname", "user.last_name")
//                    }, new string[] { "user.first_name", "user.last_name" }, new string[] { "1", "2" })
//                });
//            var json = IOnlineInboundAdapter.IConfig.Serializer.SerializeToString(config);

//            Console.WriteLine(json);
//            // {"$type":"OKPluginOIASharepoint.Config, OKPluginOIASharepoint","tenantID":"98061435-3c72-44d1-b37a-057e21f42801","siteDomain":"example.sharepoint.com","site":"play2","clientID":"3d6e9642-5430-438c-b435-34d35b323b3a","clientSecret":"/w5tskeWck6EV2sx6Mue1tkL+dSw42VdMHlPS5plohw=","useCurrentForHistoric":true,"preferredIDMapUpdateRate":"00:01:00","listConfigs":[{"$type":"OKPluginOIASharepoint.Config+ListConfig, OKPluginOIASharepoint","listID":"37800a8f-0107-445b-b70b-c783ba5a5ce3","columnConfigs":[{"$type":"OKPluginOIASharepoint.Config+ListColumnConfig, OKPluginOIASharepoint","sourceColumn":"MobilePhone","targetAttributeName":"user.mobile_phone"},{"$type":"OKPluginOIASharepoint.Config+ListColumnConfig, OKPluginOIASharepoint","sourceColumn":"Company","targetAttributeName":"user.company"},{"$type":"OKPluginOIASharepoint.Config+ListColumnConfig, OKPluginOIASharepoint","sourceColumn":"GivenName","targetAttributeName":"user.first_name"},{"$type":"OKPluginOIASharepoint.Config+ListColumnConfig, OKPluginOIASharepoint","sourceColumn":"Surname","targetAttributeName":"user.last_name"}],"identifiableAttributes":["user.first_name","user.last_name"],"searchableLayerIDs":[1,2]}],"MapperScope":"sharepoint"}
//        }


//        [Test]
//        public void BuildInternalKeycloakOIAConfig()
//        {
//            var config = new OKPluginOIAKeycloak.OnlineInboundAdapter.ConfigInternal(TimeSpan.FromSeconds(90), "keycloak_internal");
//            var json = IOnlineInboundAdapter.IConfig.Serializer.SerializeToString(config);

//            Console.WriteLine(json);
//            //{"$type":"OKPluginOIAKeycloak.OnlineInboundAdapter+ConfigInternal, OKPluginOIAKeycloak","preferredIDMapUpdateRate":"00:01:30","MapperScope":"keycloak_internal","BuilderName":"Keycloak Internal"}
//        }
//    }
//}
