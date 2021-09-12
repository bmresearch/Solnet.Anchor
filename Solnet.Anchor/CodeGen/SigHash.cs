using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Solnet.Anchor.CodeGen
{
    public static class SigHash
    {
        private static readonly SHA256 digest = SHA256.Create();


        public static ulong GetInstructionSignatureHash(string funcName, string funcNamespace)
        {
            var functionSignature = $"{funcNamespace}:{funcName.ToSnakeCase()}";

            var hash = digest.ComputeHash(Encoding.UTF8.GetBytes(functionSignature));

            var bytes = hash[..8];

            return BinaryPrimitives.ReadUInt64LittleEndian(bytes);
        }

        public static ulong GetAccountSignatureHash(string accountName)
        {
            var functionSignature = $"account:{accountName.ToPascalCase()}";

            var hash = digest.ComputeHash(Encoding.UTF8.GetBytes(functionSignature));

            var bytes = hash[..8];

            return BinaryPrimitives.ReadUInt64LittleEndian(bytes);
        }
    }
}