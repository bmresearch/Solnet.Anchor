using Solnet.Anchor.CodeGen;
using Solnet.Anchor.Converters;
using Solnet.Anchor.Models.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Solnet.Anchor.Models
{
    public class IdlField
    {
        public string Name { get; set; }

        [JsonConverter(typeof(IIdlTypeConverter))]
        public IIdlType Type { get; set; }

        internal string GenerateFieldDeclaration()
        {
            return "public " + Type.GenerateTypeDeclaration() + " " + Name.ToPascalCase() + " { get; set; }";
        }

        internal string GenerateArgumentDeclaration()
        {
            return Type.GenerateTypeDeclaration() + " " + Name;
        }

        internal Tuple<int, string, string> GetDataSize(Dictionary<string, IIdlTypeDefinitionTy> typeMap, string comulativeFieldName, string ident)
        {
            return Type.GetDataSize(typeMap, comulativeFieldName == string.Empty ? Name : $"{comulativeFieldName}.Name", ident);
        }

        internal string GenerateSerialization(Dictionary<string, IIdlTypeDefinitionTy> typeMap, string comulativeFieldName, Tuple<int, string> offset)
        {
            return Type.GenerateSerialization(typeMap, comulativeFieldName == string.Empty ? Name : $"{comulativeFieldName}.Name", offset);
        }
    }
}