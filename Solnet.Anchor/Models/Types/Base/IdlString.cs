using Solnet.Anchor.CodeGen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Solnet.Anchor.Models.Types.Base
{
    public class IdlString : IIdlType
    {
        public string GenerateTypeDeclaration()
        {
            return "string";
        }

        public Tuple<int, string, string> GetDataSize(Dictionary<string, IIdlTypeDefinitionTy> typeMap, string comulativeFieldName, string ident)
        {
            return new Tuple<int, string, string>(4, $"{comulativeFieldName}.Length", string.Empty);
        }


        public string GenerateSerialization(Dictionary<string, IIdlTypeDefinitionTy> typeMap, string comulativeFieldName, Tuple<int, string> offset)
        {

            //$""


            //return $"{Utilities.Lvl3Ident}data.";

            return "//should be an idl defined serialization";
        }
    }
}