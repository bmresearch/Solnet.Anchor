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
    }
}