using Solnet.Anchor.Models.Accounts;
using Solnet.Anchor.Models.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Solnet.Anchor.Converters
{
    public class IIdlSeedCollectionTypeConverter : JsonConverter<IList<IIdlSeed>>
    {
        public override IList<IIdlSeed> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartArray) return null;
            var converter = (JsonConverter<IIdlSeed>)options.GetConverter(typeof(IIdlSeed));
            List<IIdlSeed> seeds = new List<IIdlSeed>();

            while (reader.Read() && reader.TokenType == JsonTokenType.StartObject)
            {
                var seed = converter.Read(ref reader, typeof(IIdlSeed), options);

                seeds.Add(seed);

            }

            //array end
            //reader.Read();
            return seeds;
        }

        public override void Write(Utf8JsonWriter writer, IList<IIdlSeed> value, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }
    }
}