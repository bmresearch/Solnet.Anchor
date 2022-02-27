using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Solnet.Anchor;
using System.Collections.Generic;
using System;
using System.Buffers.Binary;
using System.Numerics;

namespace Solnet.Anchor.Test
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {
            //var res = IdlParser.ParseFile("Resources/ChatExample.json");
            var res = IdlParser.ParseFile("Resources/seq.json");
            Assert.IsNotNull(res);




            ClientGenerator c = new();

            c.GenerateSyntaxTree(res);


        }
    }
}

