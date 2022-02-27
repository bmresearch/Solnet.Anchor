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
    public class IIdlSeedTypeConverter : JsonConverter<IIdlSeed>
    {
        public override IIdlSeed Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            IIdlSeed seed = null;

            Utf8JsonReader readerCopy = reader;

            if (readerCopy.TokenType == JsonTokenType.StartObject)
            {
                readerCopy.Read();
                if (readerCopy.TokenType != JsonTokenType.PropertyName) throw new JsonException("Unexpected error value.");

                string prop = readerCopy.GetString();
                readerCopy.Read();

                if ("kind" != prop || readerCopy.TokenType != JsonTokenType.String) throw new JsonException("Unexpected value.");

                string kind = readerCopy.GetString();

                seed = kind switch
                {
                    "const" => JsonSerializer.Deserialize<IdlSeedConst>(ref reader, options),
                    "account" => JsonSerializer.Deserialize<IdlSeedAccount>(ref reader, options),
                    "arg" => JsonSerializer.Deserialize<IdlSeedArg>(ref reader, options),
                    _ => null
                };


            }
            return seed;
        }

        public override void Write(Utf8JsonWriter writer, IIdlSeed value, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }
    }
}