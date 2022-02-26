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

    }
}