using Solnet.Anchor.Converters;
using Solnet.Anchor.Models.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Solnet.Anchor.Models
{
    public class IdlEvent
    {
        public string Name { get; set; }
        public IdlEventField[] Fields { get; set; }
    }

    public class IdlEventField
    {
        public string Name { get; set; }


        [JsonConverter(typeof(IIdlTypeConverter))]
        public IIdlType Type { get; set; }

        public bool Index { get; set; }
    }
}