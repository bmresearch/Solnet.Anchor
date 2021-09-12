using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Solnet.Anchor
{
    [Generator]
    class AnchorSourceGenerator : ISourceGenerator
    {
        public void Execute(GeneratorExecutionContext context)
        {
            foreach (var file in context.AdditionalFiles)
            {
                // allow the user to override the global logging on a per-file basis
                
                if (context.AnalyzerConfigOptions.GetOptions(file).TryGetValue("build_metadata.AdditionalFiles.AnchorGen", out var perFileLoggingSwitch))
                {

                    var idl = IdlParser.ParseFile(file.Path);

                    var cg = new ClientGenerator();

                    var code = cg.GenerateCode(idl);

                    context.AddSource(idl.Name, SourceText.From(code));
                }

                // add the source with or without logging...
            }
        }

        public void Initialize(GeneratorInitializationContext context)
        {
        }
    }
}
