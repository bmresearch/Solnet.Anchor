using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Solnet.Anchor.Models.Accounts
{
    public interface IIdlAccountItem
    {
        void PreProcess(string baseNamespace, string fullGroupName);

        string GenerateFieldDeclaration(List<StringBuilder> innerTypes);
        string GenerateAccountSerialization(string v);
    }
}