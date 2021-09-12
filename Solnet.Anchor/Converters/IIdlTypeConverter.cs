using Solnet.Anchor.Models.Types;
using Solnet.Anchor.Models.Types.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Solnet.Anchor.Converters
{
    public class IIdlTypeConverter : JsonConverter<IIdlType>
    {
        public override IIdlType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                string typeName = reader.GetString();
                //reader.Read();

                return typeName switch
                {
                    "string" => new IdlString(),
                    "publicKey" => new IdlPublicKey(),
                    "bytes" => new IdlArray() { ValuesType = new IdlValueType() { TypeName = "byte" } },
                    "u128" or "i128" => new IdlBigInt() { TypeName = typeName },
                    _ => new IdlValueType() { TypeName = typeName }
                };
            }
            else if (reader.TokenType == JsonTokenType.StartObject)
            {
                reader.Read();
                if (reader.TokenType != JsonTokenType.PropertyName) throw new JsonException("Unexpected error value.");

                string typeName = reader.GetString();

                if ("defined" == typeName)
                {
                    reader.Read();
                    if (reader.TokenType != JsonTokenType.String) throw new JsonException("Unexpected error value.");

                    string definedTypeName = reader.GetString();
                    reader.Read();

                    return new IdlDefined() { TypeName = definedTypeName };
                }
                else if ("option" == typeName)
                {
                    reader.Read();
                    IIdlType innerType = Read(ref reader, typeToConvert, options);
                    if (reader.TokenType == JsonTokenType.EndObject) reader.Read();
                    return new IdlOptional() { ValuesType = innerType };
                }
                else
                {
                    reader.Read();
                    if (reader.TokenType != JsonTokenType.StartArray) throw new JsonException("Unexpected error value.");
                    reader.Read();
                    IIdlType innerType = Read(ref reader, typeToConvert, options);
                    IIdlType idlType;


                    if ("array" == typeName)
                    {
                        reader.Read();
                        int size = reader.GetInt32();
                        idlType = new IdlArray() { Size = size, ValuesType = innerType };
                    }
                    else if ("vec" == typeName)
                    {
                        idlType = new IdlArray() { ValuesType = innerType };
                    }
                    else
                    {
                        throw new JsonException("unexpected error value");
                    }

                    reader.Read();
                    reader.Read();
                    return idlType;
                }
            }
            throw new JsonException("Unexpected error value.");
        }

        public override void Write(Utf8JsonWriter writer, IIdlType value, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }
    }
}