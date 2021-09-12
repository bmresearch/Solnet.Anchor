using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Solnet.Anchor.Models.Types.Base
{
    public class IdlPublicKey : IIdlType
    {
        public string GenerateTypeDeclaration()
        {
            return "PublicKey";
        }

        public Tuple<int, string, string> GetDataSize(Dictionary<string, IIdlTypeDefinitionTy> typeMap, string comulativeFieldName, string ident)
        {
            return new Tuple<int, string, string>(32, string.Empty, string.Empty);
        }

        public string GenerateSerialization(Dictionary<string, IIdlTypeDefinitionTy> typeMap, string comulativeFieldName, Tuple<int, string> offset)
        {
            return "//should be an idl defined serialization";
        }
    }
}