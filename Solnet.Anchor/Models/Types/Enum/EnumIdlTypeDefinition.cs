using Solnet.Anchor.CodeGen;
using Solnet.Anchor.Converters;
using Solnet.Anchor.Models.Types.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Solnet.Anchor.Models.Types
{
    public class EnumIdlTypeDefinition : IIdlTypeDefinitionTy
    {
        public string Name { get; set; }

        public IEnumVariant[] Variants { get; set; }


        public bool IsPureEnum()
        {
            return Variants.All(x => x is SimpleEnumVariant);
        }
    }
}