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
    public class IIdlAccountItemConverter : JsonConverter<IIdlAccountItem[]>
    {
        public override IIdlAccountItem[] Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartArray) return null;

            List<IIdlAccountItem> accountItems = new List<IIdlAccountItem>();

            reader.Read();
            

            while (reader.TokenType == JsonTokenType.StartObject)
            {
                Utf8JsonReader readerCopy = reader;
                //IIdlAccountItem acc = 

                reader.Read();
                if (reader.TokenType != JsonTokenType.PropertyName) throw new JsonException("Unexpected error value.");

                string propertyName = reader.GetString();
                if ("name" != propertyName) throw new JsonException("Unexpected error value.");

                reader.Read();
                if (reader.TokenType != JsonTokenType.String) throw new JsonException("Unexpected error value.");

                string name = reader.GetString();

                reader.Read();
                if (reader.TokenType != JsonTokenType.PropertyName) throw new JsonException("Unexpected error value.");

                propertyName = reader.GetString();
                reader.Read();

                if ("accounts" == propertyName)
                {
                    IdlAccounts accounts = new();
                    accounts.Name = name;

                    accounts.Accounts = Read(ref reader, typeToConvert, options);
                    accountItems.Add(accounts);
                }
                else
                {
                    IdlAccount account = JsonSerializer.Deserialize<IdlAccount>(ref readerCopy, options);

                    accountItems.Add(account);
                    
                    reader = readerCopy;
                }

                // object end
                if(reader.TokenType != JsonTokenType.EndObject)
                    reader.Read();
                reader.Read();
                
            }
            //array end
            //reader.Read();
            return accountItems.ToArray();
        }

        public override void Write(Utf8JsonWriter writer, IIdlAccountItem[] value, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }
    }
}