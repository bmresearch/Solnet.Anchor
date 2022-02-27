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
    public class IIdlTypeArrayConverter : JsonConverter<IIdlType[]>
    {

        IIdlTypeConverter idlTypeConverter = new IIdlTypeConverter();

        public override IIdlType[] Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartArray) throw new JsonException("Unexpected error value.");

            reader.Read();

            List<IIdlType> types = new();

            while (reader.TokenType != JsonTokenType.EndArray)
            {
                var type = idlTypeConverter.Read(ref reader, null, options);

                types.Add(type);

                reader.Read();
            }

            return types.ToArray();
        }

        public override void Write(Utf8JsonWriter writer, IIdlType[] value, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }
    }
}