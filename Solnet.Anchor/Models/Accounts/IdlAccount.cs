using Solnet.Anchor.CodeGen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Solnet.Anchor.Models.Accounts
{
    public class IdlAccount : IIdlAccountItem
    {
        public string Name { get; set; }

        public string NamePascalCase { get; set; }

        public bool IsMut { get; set; }

        public bool IsSigner { get; set; }

        public IdlPda Pda { get; set; }

        public string GenerateAccountSerialization(string objectName)
        {
            StringBuilder sb = new();
            sb.Append(Utilities.Lvl4Ident);
            sb.Append("AccountMeta.");

            if (IsMut)
                sb.Append("Writable");
            else
                sb.Append("ReadOnly");

            sb.Append("(");
            sb.Append(objectName);
            sb.Append(".");
            sb.Append(NamePascalCase);
            sb.Append(", ");
            sb.Append(IsSigner);
            sb.Append(");");

            return sb.ToString();
        }

        public string GenerateFieldDeclaration(List<StringBuilder> innerTypes)
        {
            return "PublicKey " + NamePascalCase + " { get; set; }";
        }

        public void PreProcess(string baseNamespace, string fullGroupName)
        {
            NamePascalCase = Name.ToPascalCase();
        }
    }
}