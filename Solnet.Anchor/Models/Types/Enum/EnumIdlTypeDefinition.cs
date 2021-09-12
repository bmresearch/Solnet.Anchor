using Solnet.Anchor.CodeGen;
using Solnet.Anchor.Converters;
using Solnet.Anchor.Models.Types.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Solnet.Anchor.Models.Types
{
    public class EnumIdlTypeDefinition : IIdlTypeDefinitionTy
    {
        public string Name { get; set; }

        public IEnumVariant[] Variants { get; set; }

        public string GenerateCode()
        {
            StringBuilder sb = new();

            bool isPure = IsPureEnum();

            sb.Append(Utilities.Lvl1Ident);
            sb.Append("public enum ");
            sb.Append(Name.ToPascalCase());
            if (isPure)
                sb.AppendLine(" {");
            else
                sb.AppendLine("Type {");

            foreach (var variant in Variants)
            {
                sb.Append(Utilities.Lvl2Ident);
                sb.Append(variant.Name.ToPascalCase());
                sb.AppendLine(",");
            }

            sb.Append(Utilities.Lvl1Ident);
            sb.AppendLine("}");

            if (!isPure)
            {
                sb.Append(Utilities.Lvl1Ident);
                sb.Append("public class ");
                sb.Append(Name.ToPascalCase());
                sb.AppendLine(" {");

                sb.Append(Utilities.Lvl2Ident);
                sb.Append("public ");
                sb.Append(Name.ToPascalCase());
                sb.AppendLine("Type Type { get; set; }");

                foreach (var variant in Variants)
                {
                    if (variant is SimpleEnumVariant) continue;

                    sb.Append(Utilities.Lvl2Ident);
                    sb.Append("public ");

                    if (variant is NamedFieldsEnumVariant nf)
                    {
                        sb.Append(nf.Name.ToPascalCase());
                        sb.Append("Type ");
                        sb.Append(nf.Name.ToPascalCase());
                        sb.AppendLine("Value { get; set; }");
                    }
                    else if (variant is TupleFieldsEnumVariant tupleVariant)
                    {
                        sb.Append("Tuple<");

                        // generate tuple types

                        sb.Append(tupleVariant.Fields[0].GenerateTypeDeclaration());

                        for (int i = 1; i < tupleVariant.Fields.Length; i++)
                        {
                            sb.Append(", ");
                            sb.Append(tupleVariant.Fields[i].GenerateTypeDeclaration());
                        }

                        sb.Append("> ");

                        sb.Append(tupleVariant.Name.ToPascalCase());
                        sb.AppendLine("Value { get; set; }");
                    }
                }



                sb.Append(Utilities.Lvl1Ident);
                sb.AppendLine("}");


                foreach (var variant in Variants)
                {
                    if (variant is NamedFieldsEnumVariant namedFieldsEnumVariant)
                    {
                        sb.Append(Utilities.Lvl1Ident);
                        sb.Append("public class ");
                        sb.Append(namedFieldsEnumVariant.Name.ToPascalCase());
                        sb.Append("Type ");
                        sb.AppendLine(" {");

                        foreach (var field in namedFieldsEnumVariant.Fields)
                        {
                            sb.Append(Utilities.Lvl2Ident);
                            sb.AppendLine(field.GenerateFieldDeclaration());
                        }



                        sb.Append(Utilities.Lvl1Ident);
                        sb.AppendLine("}");
                    }
                }
            }


            return sb.ToString();
        }

        public bool IsPureEnum()
        {
            return Variants.All(x => x is SimpleEnumVariant);
        }
    }
}