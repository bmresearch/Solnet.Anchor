using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Solnet.Anchor.Models.Types.Base
{
    public class IdlDefined : IIdlType
    {
        public string TypeName { get; set; }

        public string GenerateSerialization(Dictionary<string, IIdlTypeDefinitionTy> typeMap, string comulativeFieldName, Tuple<int, string> offset)
        {
            return "//should be an idl defined serialization";
        }

        public string GenerateTypeDeclaration()
        {
            return TypeName;
        }

        public Tuple<int, string, string> GetDataSize(Dictionary<string, IIdlTypeDefinitionTy> typeMap, string comulativeFieldName, string ident)
        {
            return new(0, "", "//should be an idl defined");
        }
    }
}