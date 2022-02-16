using Solnet.Anchor.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Solnet.Anchor.Models.Accounts
{
    public class IdlPda
    {
        [JsonConverter(typeof(IIdlSeedCollectionTypeConverter))]
        public IList<IIdlSeed> Seeds { get; set; }

        [JsonConverter(typeof(IIdlSeedTypeConverter))]
        public IIdlSeed ProgramId { get; set; }
    }
}
