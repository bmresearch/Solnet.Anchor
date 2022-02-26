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
        private const string IDL_SEED = "anchor:idl";

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
            PublicKey.TryFindProgramAddress(Array.Empty<byte[]>(), pk, out var add, out byte _);

            PublicKey.TryCreateWithSeed(add, IDL_SEED, pk, out var derivedAddress);


            var res = client.GetAccountInfo(derivedAddress);

            if(!res.WasSuccessful || res.Result.Value == null || res.Result.Value.Data == null)
            {
                Console.WriteLine($"Unable to fetch IDL for program {pk.Key}, from expected IDL acc {add.Key}");
                return null;
            }


            var accBytes = Convert.FromBase64String(res.Result.Value.Data[0]);

            var len = new ReadOnlySpan<byte>(accBytes).GetU32(40);

            var idlBytes = accBytes[44..((int)len + 44)];

            var idlBytesDecompresssed = Decompress(idlBytes);

            var idlStr = Encoding.UTF8.GetString(idlBytesDecompresssed);

            var idl = Parse(idlStr);

            return idl;
        }

        private static byte[] Decompress(byte[] input)
        {
            var output = new MemoryStream();


            using (var compressStream = new MemoryStream(input))
            using (var decompressor = new ZLibStream(compressStream, CompressionMode.Decompress))
                decompressor.CopyTo(output);

            return output.ToArray();
        }
    }
}