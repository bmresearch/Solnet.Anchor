using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Solnet.Anchor.Models.Types.Base
{
    public class IdlValueType : IIdlType
    {
        public string TypeName { get; set; }

        public string GenerateTypeDeclaration()
        => TypeName switch
        {
            "i8" => "sbyte",
            "u8" => "byte",
            "i16" => "short",
            "u16" => "ushort",
            "i32" => "int",
            "u32" => "uint",
            "i64" => "long",
            "u64" => "ulong",
            "bool" => "bool",
            _ => throw new Exception("Something wrong occurred")
        };

        public Tuple<int, string, string> GetDataSize(Dictionary<string, IIdlTypeDefinitionTy> typeMap, string comulativeFieldName, string ident)
        => TypeName switch
        {
            "i8" or "u8" or "bool" => new Tuple<int, string, string>(1, string.Empty, string.Empty),
            "i16" or "u16" => new Tuple<int, string, string>(2, string.Empty, string.Empty),
            "i32" or "u32" => new Tuple<int, string, string>(4, string.Empty, string.Empty),
            "i64" or "u64" => new Tuple<int, string, string>(8, string.Empty, string.Empty),
            _ => throw new Exception("Something wrong occurred")

        };

        public string GenerateSerialization(Dictionary<string, IIdlTypeDefinitionTy> typeMap, string comulativeFieldName, Tuple<int, string> offset)
        {
            return "//should be an idl defined serialization";
        }
    }


}