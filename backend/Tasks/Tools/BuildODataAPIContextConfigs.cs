using Microsoft.AspNetCore.Identity;
using NUnit.Framework;
using Omnikeeper.Base.Entity;
using System;

namespace Tasks.Tools
{
    [Explicit]
    class BuildODataAPIContextConfigs
    {
        [Test]
        public void Build()
        {
            var passwordHasher = new PasswordHasher<string>();
            var username = "username";
            var password = "password";
            var hashedPW = passwordHasher.HashPassword(username, password);
            var config = new ODataAPIContext.ConfigV4("7", new string[] { "1", "2", "3", "4", "5", "6", "7" }, new ODataAPIContext.ContextAuthBasic(username, hashedPW));
            var json = ODataAPIContext.ConfigSerializer.SerializeToString(config);

            Console.WriteLine(json); // {"$type":"ConfigV4","WriteLayerID":"7","ReadLayerset":["1","2","3","4","5","6","7"],"ContextAuth":{"type":"ContextAuthBasic","Username":"username","PasswordHashed":"AQAAAAEAACcQAAAAEMFASJkPeibuvR8u8Gi0\u002B2r4mLUO3ww7gCtORjmWhKT7rq8p0wUMdrPzPDHIbRz3Zg=="}}
        }
    }
}
