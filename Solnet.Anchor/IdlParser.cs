using Solnet.Anchor.Models;
using System;
using System.IO;
using System.Text.Json;

namespace Solnet.Anchor
{
    public static class IdlParser
    {

        public static Idl Parse(string idl)
        {
            var res = JsonSerializer.Deserialize<Idl>(idl, new JsonSerializerOptions() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
            return res;
        }

        public static Idl ParseFile(string idlFile)
        {
            return Parse(File.ReadAllText(idlFile));
        }
    }
}