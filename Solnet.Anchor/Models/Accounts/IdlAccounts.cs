using Solnet.Anchor.CodeGen;
using Solnet.Anchor.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Solnet.Anchor.Models.Accounts
{
    public class IdlAccounts : IIdlAccountItem
    {
        public string Name { get; set; }

        public string FullName { get; set; }

        public string NamePascalCase { get; set; }

        [JsonConverter(typeof(IIdlAccountItemConverter))]
        public IIdlAccountItem[] Accounts { get; set; }


        public void PreProcess(string baseNamespace, string fullGroupName)
        {
            NamePascalCase = Name.ToPascalCase();
            FullName = fullGroupName + NamePascalCase;

            foreach (var account in Accounts)
            {
                account.PreProcess(baseNamespace, FullName);
            }
        }

        public string GenerateFieldDeclaration(List<StringBuilder> innerTypes)
        {
            GenerateInnerType(innerTypes);
            return FullName + "Accounts " + NamePascalCase + " { get; set;}";
        }


        private void GenerateInnerType(List<StringBuilder> innerTypes)
        {
            StringBuilder sb = new();

            sb.Append(Utilities.Lvl1Ident);
            sb.Append("public class ");

            sb.Append(FullName);
            sb.AppendLine("Accounts {");

            foreach (var acc in Accounts)
            {
                sb.Append(Utilities.PublicFieldModifierIdent);

                sb.AppendLine(acc.GenerateFieldDeclaration(innerTypes));
            }

            sb.Append(Utilities.Lvl1Ident);
            sb.AppendLine("}");

            innerTypes.Add(sb);
        }

        public string GenerateAccountSerialization(string objectName)
        {
            StringBuilder sb = new();

            foreach (var acc in Accounts)
            {
                sb.AppendLine(acc.GenerateAccountSerialization(objectName + "." + NamePascalCase));
            }
            return sb.ToString();
        }
    }
}