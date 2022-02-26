using CommandLine;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Solnet.Anchor;
using System.IO;
using Solnet.Anchor.Models;

public class AnchorSourceGenerator
{
    static int Main(string[] args)
    {
        return Parser.Default.ParseArguments<CommandLineOptions>(args)
            .MapResult((CommandLineOptions opts) =>
            {
                Idl idl = null;
                if(opts.File != null)
                {
                    idl = IdlParser.ParseFile(opts.File);
                }
                else
                {
                    idl = IdlParser.ParseProgram(new Solnet.Wallet.PublicKey(opts.Address));
                }

                if(idl == null)
                {
                    Console.WriteLine("No IDL was generated. exiting");
                }

                idl.DefaultProgramAddress = opts.Address;

                ClientGenerator cg = new ClientGenerator();

                var code = cg.GenerateCode(idl);

                Console.WriteLine(idl.NamePascalCase);

                if(!string.IsNullOrWhiteSpace(opts.Out))
                    File.WriteAllText(opts.Out, code);

                if (opts.StdOut)
                    Console.Write(code);

                return 0;
            },
            (IEnumerable<Error> errors) =>
            {
                if (!errors.Any(x => x is HelpRequestedError or VersionRequestedError or HelpVerbRequestedError)) return -1; // Invalid arguments
                return 0;
            }); 
    }
}
public class CommandLineOptions
{
    [Option('a', "address", Group = "source", HelpText = "Anchor Program Address")]
    public string Address { get; set; }


    [Option('i', "idl", Group = "source", HelpText = "Idl Source file")]
    public string File { get; set; }


    [Option('o', "output", Group = "output", HelpText = "File to write generated C# code")]
    public string Out { get; set; }

    [Option('s', "stdout", Group = "output", HelpText = "Write generated C# code to stdout")]
    public bool StdOut { get; set; }
}