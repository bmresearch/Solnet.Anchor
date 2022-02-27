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

        public string NamePascalCase => Name.ToPascalCase();

        public bool IsMut { get; set; }

        public bool IsSigner { get; set; }

        public IdlPda Pda { get; set; }

    }
}