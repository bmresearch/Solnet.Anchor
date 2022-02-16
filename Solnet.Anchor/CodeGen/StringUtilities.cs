using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Solnet.Anchor.CodeGen
{
    public static class StringUtilities
    {
        public static string ToPascalCase(this string name)
        {
            var chars = name.ToArray();

            if (char.IsLower(chars[0]))
                chars[0] = char.ToUpper(chars[0]);
            
            int uc = 0;
            for (int i = 1; i < chars.Length; i++)
            {
                if(chars[i] == '_')
                {
                    uc++;
                    i++;
                    chars[i - uc] = char.ToUpper(chars[i]);
                }
                else if (uc > 0)
                {
                    chars[i - uc] = chars[i];
                }
            }

            return new string(chars, 0, chars.Length - uc);
        }
        
        public static string ToCamelCase(this string name)
        {
            var chars = name.ToArray();

            if (char.IsUpper(chars[0]))
                chars[0] = char.ToLower(chars[0]);
            return new string(chars);
        }

        public static string ToSnakeCase(this string name)
        {
            if (name.Length < 2) return name.ToLowerInvariant();

            StringBuilder sb = new(name.Length * 2);

            sb.Append(char.ToLowerInvariant(name[0]));

            for (int i = 1; i < name.Length; i++)
            {
                if (char.IsUpper(name[i]))
                {
                    sb.Append("_");
                    sb.Append(char.ToLowerInvariant(name[i]));
                }
                else
                {
                    sb.Append(name[i]);
                }
            }
            return sb.ToString();
        }
    }
}