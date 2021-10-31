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


public class AnchorSourceGenerator
{
    static int Main(string[] args)
    {
        return Parser.Default.ParseArguments<CommandLineOptions>(args)
            .MapResult((CommandLineOptions opts) =>
            {
                var idl = IdlParser.ParseFile(opts.Idl);

                ClientGenerator cg = new ClientGenerator();

                var code = cg.GenerateCode(idl);

                File.WriteAllText(opts.Out, code);
                return 0;
            },
            _ => -1); // Invalid arguments
    }
}
public class CommandLineOptions
{
    [Value(index: 0, Required = true, HelpText = "Idl source file.")]
    public string Idl { get; set; }


    [Value(index: 1, Required = true, HelpText = "C# output file.")]
    public string Out { get; set; }
}