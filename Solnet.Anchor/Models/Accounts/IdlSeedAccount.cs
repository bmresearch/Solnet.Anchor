using Solnet.Anchor.Converters;
using Solnet.Anchor.Models.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Solnet.Anchor.Models.Accounts
{
    public class IdlSeedAccount : IIdlSeed
    {
        [JsonConverter(typeof(IIdlTypeConverter))]
        public IIdlType Type { get; set; }

        public string Account { get; set; }

        public string Path { get; set; }
    }
}
