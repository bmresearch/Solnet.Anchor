using Solnet.Anchor.CodeGen;
using Solnet.Anchor.Converters;
using Solnet.Anchor.Models.Accounts;
using Solnet.Anchor.Models.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Solnet.Anchor.Models
{
    public class Idl
    {
        [JsonIgnore]
        public string DefaultProgramAddress { get; set; }

        public string Version { get; set; }
        public string Name { get; set; }

        public string NamePascalCase => Name.ToPascalCase();

        public IdlInstruction[] Instructions { get; set; }


        [JsonConverter(typeof(IIdlTypeDefinitionTyConverter))]
        public IIdlTypeDefinitionTy[] Accounts { get; set; }

        [JsonConverter(typeof(IIdlTypeDefinitionTyConverter))]
        public IIdlTypeDefinitionTy[] Types { get; set; }

        public IdlErrorCode[] Errors { get; set; }

        public IdlEvent[] Events { get; set; }

    }

}