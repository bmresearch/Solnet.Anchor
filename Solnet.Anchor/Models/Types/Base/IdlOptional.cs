using Solnet.Anchor.CodeGen;
using Solnet.Anchor.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Solnet.Anchor.Models.Types.Base
{
    public class IdlOptional : IIdlType
    {

        [JsonConverter(typeof(IIdlTypeConverter))]
        public IIdlType ValuesType { get; set; }

        public string GenerateTypeDeclaration()
        {
            string typeDecl = ValuesType.GenerateTypeDeclaration();

            if (ValuesType is IdlValueType)
                typeDecl += "?";

            return typeDecl;
        }

        public Tuple<int, string, string> GetDataSize(Dictionary<string, IIdlTypeDefinitionTy> typeMap, string comulativeFieldName, string ident)
        {
            var res = ValuesType.GetDataSize(typeMap, comulativeFieldName + (ValuesType is IdlValueType ? ".Value" : ""), ident);

            string condition = ValuesType is IdlValueType ? $"{comulativeFieldName}.HasValue" : $"{comulativeFieldName} != null";

            if (res.Item2 == string.Empty && res.Item3 == string.Empty)
            {
                return new(1, $"({condition} ? {res.Item1} : 0)", string.Empty);
            }
            else if (res.Item2 != string.Empty && res.Item3 == string.Empty)
            {
                return new(1, $"({condition} ? {res.Item1} + {res.Item2} : 0)", string.Empty);
            }

            // else commit sudoku

            StringBuilder sb = new();


            sb.Append(res.Item3);


            sb.Append(Utilities.Lvl3Ident);
            sb.Append($"int {comulativeFieldName.Replace("[", "_").Replace("]", "_").Replace(".", "_")}Size = {condition} ? {res.Item1} + {res.Item2} : {res.Item1};");


            return new Tuple<int, string, string>(1, $"{comulativeFieldName.Replace("[", "_").Replace("]", "_").Replace(".", "_")}Size", sb.ToString());
        }

        public string GenerateSerialization(Dictionary<string, IIdlTypeDefinitionTy> typeMap, string comulativeFieldName, Tuple<int, string> offset)
        {
            return "//should be an idl defined serialization";
        }
    }
}