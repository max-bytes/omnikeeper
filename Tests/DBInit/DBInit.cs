using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using Tests.Integration;

namespace Tests.DBInit
{
    [Explicit]
    [Ignore("Only manual")]
    class DBInit
    {
        [Test]
        public void Run()
        {
            DBSetup._Setup("landscape_prototype");
        }
    }
}
