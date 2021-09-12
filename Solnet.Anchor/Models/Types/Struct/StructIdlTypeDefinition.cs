using Solnet.Anchor.CodeGen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Solnet.Anchor.Models.Types
{
    public class StructIdlTypeDefinition : IIdlTypeDefinitionTy
    {
        public string Name { get; set; }

        public IdlField[] Fields { get; set; }


        public string GenerateCode()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append(Utilities.Lvl1Ident);
            sb.Append("public class ");
            sb.Append(Name.ToPascalCase());
            sb.AppendLine(" {");

            foreach (var field in Fields)
            {
                sb.Append(Utilities.Lvl2Ident);
                sb.AppendLine(field.GenerateFieldDeclaration());
            }


            sb.Append(Utilities.Lvl1Ident);
            sb.AppendLine("}");
            return sb.ToString();
        }
    }
}