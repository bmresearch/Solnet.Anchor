using Solnet.Anchor.CodeGen;
using Solnet.Anchor.Converters;
using Solnet.Anchor.Models.Accounts;
using Solnet.Anchor.Models.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Solnet.Anchor.Models
{
    public class Idl
    {
        public string Version { get; set; }
        public string Name { get; set; }

        public string NamePascalCase { get; set; }

        public IdlInstruction[] Instructions { get; set; }


        [JsonConverter(typeof(IIdlTypeDefinitionTyConverter))]
        public IIdlTypeDefinitionTy[] Accounts { get; set; }

        [JsonConverter(typeof(IIdlTypeDefinitionTyConverter))]
        public IIdlTypeDefinitionTy[] Types { get; set; }

        public IdlErrorCode[] Errors { get; set; }

        public IdlEvent[] Events { get; set; }


        private Dictionary<string, IIdlTypeDefinitionTy> typesMap = new();

        public void PreProcess(string baseNamespace, string accountsNamespace, string typesNamespace, string errorsNamespace, string eventsNamespace)
        {
            NamePascalCase = Name.ToPascalCase();
            foreach (var instruction in Instructions)
            {
                instruction.PreProcess(baseNamespace, "global");
            }

            Array.ForEach(Types, x => typesMap.Add(x.Name, x));
        }


        public string GenerateCode()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("using Solnet.Rpc;");
            sb.AppendLine("using Solnet.Programs;");

            sb.Append("namespace ");
            sb.Append(Name.ToPascalCase());
            sb.AppendLine(" {");

            if (Accounts != null)
            {
                sb.AppendLine("#region Accounts");

                foreach (var acc in Accounts)
                {
                    // pass string builder instead?
                    sb.AppendLine(acc.GenerateCode());
                }

                sb.AppendLine("#endregion");
            }

            if (Types != null)
            {

                sb.AppendLine("#region Types");


                foreach (var type in Types)
                {
                    // pass string builder instead?
                    sb.AppendLine(type.GenerateCode());
                }

                sb.AppendLine("#endregion");

            }


            if (Instructions != null)
            {

                sb.Append(Utilities.Lvl1Ident);
                sb.Append("public static class ");
                sb.Append(NamePascalCase);
                sb.AppendLine("Program {");

                foreach (var instruction in Instructions)
                {
                    sb.Append(Utilities.Lvl2Ident);
                    sb.Append("public static TransactionInstruction ");
                    sb.Append(instruction.Name);
                    sb.Append("(");

                    /// params
                    /// 

                    sb.Append(instruction.Name);
                    sb.Append("Accounts accounts");

                    int dataSize = 8;


                    List<string> dynamicSize = new();
                    List<string> tmpCalcs = new();
                    List<string> serializationCode = new();

                    string dynamicSizeComulative = "";

                    if (instruction.Args != null && instruction.Args.Length > 0)
                    {
                        foreach (var arg in instruction.Args)
                        {
                            sb.Append(", ");
                            sb.Append(arg.GenerateArgumentDeclaration());

                            Tuple<int, string, string> size = arg.GetDataSize(typesMap, string.Empty, Utilities.Lvl3Ident);

                            dataSize += size.Item1;

                            if (size.Item2 != string.Empty)
                            {
                                dynamicSize.Add(size.Item2);
                                if (dynamicSizeComulative == string.Empty)
                                {
                                    dynamicSizeComulative = size.Item2;
                                }
                                else
                                {
                                    dynamicSizeComulative = " + " + size.Item2;
                                }
                            }

                            if (size.Item3 != string.Empty)
                                tmpCalcs.Add(size.Item3);



                            serializationCode.Add(arg.GenerateSerialization(typesMap, string.Empty, new Tuple<int, string>(dataSize, dynamicSizeComulative)));
                        }
                    }


                    sb.AppendLine(")");
                    sb.Append(Utilities.Lvl2Ident);
                    sb.AppendLine("{");


                    /// 
                    sb.Append(Utilities.Lvl3Ident);
                    sb.AppendLine("List<AccountMeta> keys = new()");
                    sb.Append(Utilities.Lvl3Ident);
                    sb.AppendLine("{");

                    foreach (var acc in instruction.Accounts)
                    {
                        sb.AppendLine(acc.GenerateAccountSerialization("accounts"));
                    }

                    sb.Append(Utilities.Lvl3Ident);
                    sb.AppendLine("}");


                    foreach (var tmp in tmpCalcs)
                        sb.AppendLine(tmp);

                    sb.Append(Utilities.Lvl3Ident);

                    sb.Append("var data = new byte[");
                    sb.Append(dataSize);

                    foreach (var tmp in dynamicSize)
                    {
                        sb.Append(" + ");
                        sb.Append(tmp);
                    }

                    sb.AppendLine("];");

                    sb.Append(Utilities.Lvl3Ident);
                    sb.Append("data.WriteU64(");
                    sb.Append(instruction.InstructionSignatureHash);
                    sb.AppendLine(", 0); // instruction hash");



                    foreach (var ser in serializationCode)
                    {
                        sb.AppendLine(ser);
                    }


                    sb.Append(Utilities.Lvl2Ident);
                    sb.AppendLine("}");
                }

                sb.Append(Utilities.Lvl1Ident);
                sb.AppendLine("}");


                /// account groups

                foreach (var instruction in Instructions)
                {
                    List<StringBuilder> innerTypes = new();
                    sb.Append(Utilities.Lvl1Ident);
                    sb.Append("public class ");

                    sb.Append(instruction.Name);
                    sb.AppendLine("Accounts {");

                    foreach (var acc in instruction.Accounts)
                    {
                        sb.Append(Utilities.PublicFieldModifierIdent);

                        sb.AppendLine(acc.GenerateFieldDeclaration(innerTypes));
                    }

                    sb.Append(Utilities.Lvl1Ident);
                    sb.AppendLine("}");

                    innerTypes.ForEach(x => sb.Append(x));
                }
            }


            sb.AppendLine("}");


            return sb.ToString();
        }

        private List<IdlAccounts> GetAccountGroups(IIdlAccountItem[] accountItems)
        {
            List<IdlAccounts> accounts = new List<IdlAccounts>();

            foreach (var acc in accountItems)
            {
                if (acc is IdlAccounts accountsGroup)
                {
                    accounts.Add(accountsGroup);
                    accounts.AddRange(GetAccountGroups(accountsGroup.Accounts));
                }
            }
            return accounts;
        }
    }






}