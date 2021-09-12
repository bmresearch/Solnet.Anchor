using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Solnet.Anchor.Models.Types.Base
{
    public class IdlBigInt : IIdlType
    {
        public string TypeName { get; set; }

        public string GenerateSerialization(Dictionary<string, IIdlTypeDefinitionTy> typeMap, string comulativeFieldName, Tuple<int, string> offset)
        {
            return "";
        }

        public string GenerateTypeDeclaration()
        {
            return "BigInteger";
        }

        public Tuple<int, string, string> GetDataSize(Dictionary<string, IIdlTypeDefinitionTy> typeMap, string comulativeFieldName, string ident)
        {
            return new(16, string.Empty, string.Empty);
        }
    }
}