using Solnet.Wallet;
using Solnet.Rpc;
using Solnet.Programs.Utilities;
using System;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace Solnet.Anchor
{
    public static class IdlRetriever
    {
        private const string IDL_SEED = "anchor:idl";
        
        public static string GetIdl(PublicKey program, IRpcClient client = default)
        {
            client ??= ClientFactory.GetClient(Cluster.MainNet);

            var address = GetIdlAccount(program);

            var res = client.GetAccountInfo(address);

            if(!res.WasSuccessful || res.Result.Value == null || res.Result.Value.Data == null)
            {
                Console.WriteLine($"Unable to fetch IDL for program {program.Key}, from expected IDL acc {address.Key}");
                return null;
            }


            var accBytes = Convert.FromBase64String(res.Result.Value.Data[0]);

            var len = new ReadOnlySpan<byte>(accBytes).GetU32(40);

            var idlBytes = accBytes[44..((int)len + 44)];

            var idlBytesDecompresssed = Decompress(idlBytes);

            var idlStr = Encoding.UTF8.GetString(idlBytesDecompresssed);

            return idlStr;

        }

        public static PublicKey GetIdlAccount(PublicKey program)
        {
            PublicKey.TryFindProgramAddress(Array.Empty<byte[]>(), program, out var add, out byte _);

            PublicKey.TryCreateWithSeed(add, IDL_SEED, program, out var derivedAddress);

            return derivedAddress;
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