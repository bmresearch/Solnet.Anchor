using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Solnet.Anchor.Models.Types
{
    public interface IIdlTypeDefinitionTy
    {
        string Name { get; }
        string GenerateCode();
    }
}