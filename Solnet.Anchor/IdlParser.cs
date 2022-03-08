using Solnet.Anchor.Converters;
using Solnet.Anchor.Models;
using Solnet.Rpc;
using Solnet.Rpc.Utilities;
using Solnet.Wallet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.IO.Compression;
using System.Text;
using Solnet.Programs.Utilities;

namespace Solnet.Anchor
{
    public static class IdlParser
    {

        public static Idl Parse(string idl)
        {
            var res = JsonSerializer.Deserialize<Idl>(idl, new JsonSerializerOptions()
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                Converters = { new IIdlSeedTypeConverter() }
            });
            return res;
        }

        public static Idl ParseFile(string idlFile)
        {
            return Parse(File.ReadAllText(idlFile));
        }

        public static Idl ParseProgram(PublicKey pk)
        {
            return ParseProgram(pk, ClientFactory.GetClient(Cluster.MainNet));
        }

        public static Idl ParseProgram(PublicKey pk, IRpcClient client)
        {
            var idlStr = IdlRetriever.GetIdl(pk, client);
            var idl = Parse(idlStr);

            return idl;
        }

    }
}