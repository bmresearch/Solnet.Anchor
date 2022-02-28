using Solnet.Anchor.Models;
using Solnet.Anchor.Models.Types;
using Solnet.Anchor.Models.Types.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Solnet.Anchor.Converters
{
    public class IIdlTypeDefinitionTyConverter : JsonConverter<IIdlTypeDefinitionTy[]>
    {
        public override IIdlTypeDefinitionTy[] Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {

            if (reader.TokenType != JsonTokenType.StartArray) return null;
            reader.Read();

            List<IIdlTypeDefinitionTy> types = new();

            while (reader.TokenType == JsonTokenType.StartObject)
            {
                reader.Read();
                if (reader.TokenType != JsonTokenType.PropertyName) throw new JsonException("Unexpected error value.");

                string propertyName = reader.GetString();
                if ("name" != propertyName) throw new JsonException("Unexpected error value.");

                reader.Read();
                if (reader.TokenType != JsonTokenType.String) throw new JsonException("Unexpected error value.");

                string typeName = reader.GetString();


                reader.Read();
                if (reader.TokenType != JsonTokenType.PropertyName) throw new JsonException("Unexpected error value.");

                propertyName = reader.GetString();
                if ("type" != propertyName) throw new JsonException("Unexpected error value.");

                reader.Read();
                if (reader.TokenType != JsonTokenType.StartObject) throw new JsonException("Unexpected error value.");

                reader.Read();
                if (reader.TokenType != JsonTokenType.PropertyName) throw new JsonException("Unexpected error value.");

                propertyName = reader.GetString();
                if ("kind" != propertyName) throw new JsonException("Unexpected error value.");

                reader.Read();
                if (reader.TokenType != JsonTokenType.String) throw new JsonException("Unexpected error value.");

                string typeType = reader.GetString();

                reader.Read();
                if (reader.TokenType != JsonTokenType.PropertyName) throw new JsonException("Unexpected error value.");

                propertyName = reader.GetString();

                reader.Read();

                if ("struct" == typeType)
                {
                    if ("fields" != propertyName) throw new JsonException("Unexpected error value.");
                    var res = JsonSerializer.Deserialize<IdlField[]>(ref reader, options);

                    types.Add(new StructIdlTypeDefinition() { Name = typeName, Fields = res });
                    reader.Read(); //end array
                }
                else
                {
                    if ("variants" != propertyName) throw new JsonException("Unexpected error value.");

                    List<IEnumVariant> variants = new();

                    reader.Read();
                    while (reader.TokenType == JsonTokenType.StartObject)
                    {

                        //if (reader.TokenType != JsonTokenType.StartObject) throw new JsonException("Unexpected error value.");


                        Utf8JsonReader tmp = reader;

                        tmp.Read();

                        if (tmp.TokenType != JsonTokenType.PropertyName) throw new JsonException("Unexpected error value.");
                        propertyName = tmp.GetString();

                        if ("name" != propertyName) throw new JsonException("Unexpected error value.");


                        tmp.Read();

                        if (tmp.TokenType != JsonTokenType.String) throw new JsonException("Unexpected error value.");
                        string variantName = tmp.GetString();

                        tmp.Read();

                        if (tmp.TokenType != JsonTokenType.PropertyName)
                        {
                            variants.Add(new SimpleEnumVariant() { Name = variantName });
                            reader = tmp;
                        }
                        else
                        {

                            propertyName = tmp.GetString();

                            if ("fields" != propertyName) throw new JsonException("Unexpected error value.");

                            tmp.Read();


                            if (tmp.TokenType != JsonTokenType.StartArray) throw new JsonException("Unexpected error value.");

                            tmp.Read();

                            bool isNamedVariant = false;

                            if (tmp.TokenType == JsonTokenType.StartObject)
                            {
                                tmp.Read();

                                while (tmp.TokenType != JsonTokenType.EndObject)
                                {
                                    if (tmp.TokenType == JsonTokenType.StartObject || tmp.TokenType == JsonTokenType.StartArray)
                                        tmp.Skip();

                                    if (tmp.TokenType == JsonTokenType.PropertyName
                                        && tmp.GetString() == "name")
                                    {
                                        isNamedVariant = true;
                                        break;
                                    }
                                    tmp.Read();
                                }
                            }

                            if(isNamedVariant)
                            { 
                                var variant = JsonSerializer.Deserialize<NamedFieldsEnumVariant>(ref reader, options);

                                variants.Add(variant);
                            }
                            else
                            {
                                var variant = JsonSerializer.Deserialize<TupleFieldsEnumVariant>(ref reader, options);
                                variants.Add(variant);
                            }
                        }
                        reader.Read();
                    }

                    types.Add(new EnumIdlTypeDefinition() { Name = typeName, Variants = variants.ToArray() });

                    reader.Read(); // end array
                }

                // end type inner property
                reader.Read();
                // end type 
                reader.Read();
            }
            return types.ToArray();
        }

        public override void Write(Utf8JsonWriter writer, IIdlTypeDefinitionTy[] value, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }
    }
}