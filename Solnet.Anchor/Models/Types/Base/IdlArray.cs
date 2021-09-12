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
    public class IdlArray : IIdlType
    {

        [JsonConverter(typeof(IIdlTypeConverter))]
        public IIdlType ValuesType { get; set; }

        public int? Size { get; set; }

        public string GenerateSerialization(Dictionary<string, IIdlTypeDefinitionTy> typeMap, string comulativeFieldName, Tuple<int, string> offset)
        {
            var counterName = comulativeFieldName.Replace("[", "_").Replace("]", "_").Replace(".", "_");
            var iteratorName = counterName + "Idx";
            counterName += "Counter";

            StringBuilder sb = new();

            sb.Append(Utilities.Lvl3Ident);
            sb.Append("var ");
            sb.Append(counterName);
            sb.AppendLine(" = 0;");

            sb.Append(Utilities.Lvl3Ident);
            sb.Append("for(int ");
            sb.Append(iteratorName);
            sb.Append(" = 0; ");
            sb.Append(iteratorName);
            sb.Append(" < ");

            if (Size.HasValue)
            {
                sb.Append(Size.Value);
                sb.AppendLine("; i++)");
            }
            else
            {
                sb.Append(comulativeFieldName);
                sb.AppendLine(".Length; i++)");
            }
            sb.Append(Utilities.Lvl3Ident);
            sb.AppendLine("{");

            sb.AppendLine(ValuesType.GenerateSerialization(typeMap, $"{comulativeFieldName}[{iteratorName}]", new(offset.Item1, counterName)).Replace(Utilities.Lvl3Ident, Utilities.Lvl4Ident));



            sb.Append(Utilities.Lvl3Ident);
            sb.AppendLine("}");


            if (offset.Item2 != string.Empty)
            {
                sb.Append(Utilities.Lvl3Ident);
                sb.Append(offset.Item2);
                sb.Append(" += ");
                sb.Append(counterName);
                sb.AppendLine(";");
            }

            return sb.ToString();

        }

        public string GenerateTypeDeclaration()
        {
            return ValuesType.GenerateTypeDeclaration() + "[]";
        }
        public Tuple<int, string, string> GetDataSize(Dictionary<string, IIdlTypeDefinitionTy> typeMap, string comulativeFieldName, string ident)
        {
            var counterName = comulativeFieldName.Replace("[", "_").Replace("]", "_").Replace(".", "_");
            var iteratorName = counterName + "Idx";

            counterName += "Counter";

            var res = ValuesType.GetDataSize(typeMap, $"{comulativeFieldName}[{iteratorName}]", ident);


            if (res.Item2 == string.Empty && res.Item3 == string.Empty)
            {
                if (Size.HasValue)
                    return new(Size.Value * res.Item1, string.Empty, string.Empty);
                else
                    return new(4, $"{comulativeFieldName}.Length{(res.Item1 > 1 ? $" * {res.Item1}" : "")}", string.Empty);
            }
            StringBuilder sb = new();

            sb.Append(Utilities.Lvl3Ident);
            sb.Append("var ");
            sb.Append(counterName);
            sb.AppendLine(" = 0;");

            sb.Append(Utilities.Lvl3Ident);
            sb.Append("for(int ");
            sb.Append(iteratorName);
            sb.Append(" = 0; ");
            sb.Append(iteratorName);
            sb.Append(" < ");

            if (Size.HasValue)
            {
                sb.Append(Size.Value);
                sb.AppendLine("; i++)");
            }
            else
            {
                sb.Append(comulativeFieldName);
                sb.AppendLine(".Length; i++)");
            }
            sb.Append(Utilities.Lvl3Ident);
            sb.AppendLine("{");

            if (res.Item3 != string.Empty)
            {
                sb.AppendLine(res.Item3.Replace(Utilities.Lvl3Ident, Utilities.Lvl3Ident + Utilities.Lvl1Ident));
            }


            sb.Append(Utilities.Lvl4Ident);
            sb.Append(counterName);
            sb.Append(" += ");
            sb.Append(res.Item2);

            if (!Size.HasValue)
                sb.AppendLine(" + 4;");
            else
                sb.AppendLine(";");

            sb.Append(Utilities.Lvl3Ident);
            sb.AppendLine("}");

            if (Size.HasValue)
            {
                return new Tuple<int, string, string>(0, counterName, sb.ToString());
            }
            return new(4, counterName, sb.ToString());

        }
    }
}