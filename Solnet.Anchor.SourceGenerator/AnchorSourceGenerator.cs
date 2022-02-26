using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace Solnet.Anchor.SourgeGenerator
{
    [Generator]
    public class AnchorSourceGenerator : ISourceGenerator
    {
        public void Execute(GeneratorExecutionContext context)
        {
            // global logging from project file
            List<string> addresses = new List<string>();
            Dictionary<AdditionalText, string> files = new Dictionary<AdditionalText, string>();
            bool success = false;

            try
            {
                var p = Process.Start(new ProcessStartInfo("dotnet", "anchorgen --version") { RedirectStandardError = true, RedirectStandardOutput = true, UseShellExecute = false, CreateNoWindow = true, WindowStyle = ProcessWindowStyle.Hidden });

                p.WaitForExit();

                if (p.ExitCode == 0)
                    success = true;
            }
            catch (Exception)
            {
            }

            if (!success)
            {
                context.ReportDiagnostic(Diagnostic.Create(new DiagnosticDescriptor("ANCHOR-GENERATOR-01", "Tool Error", "dotnet anchorgen not found. Please install the tool using 'dotnet install Solnet.Anchor.SourceGenerator'", "Tool.Dependency", DiagnosticSeverity.Error, true), null));
                return;
            }


            if (context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("build_property.AnchorGenerator", out var anchorGenerateAddresses) && !string.IsNullOrEmpty(anchorGenerateAddresses))
            {
                addresses = anchorGenerateAddresses.Split(',').ToList();
            }

            foreach (var file in context.AdditionalFiles)
            {

                if (context.AnalyzerConfigOptions.GetOptions(file).TryGetValue("build_metadata.AdditionalFiles.AnchorGenerate", out var anchorGen)
                    && anchorGen.Equals("true", StringComparison.OrdinalIgnoreCase))
                {
                    context.AnalyzerConfigOptions.GetOptions(file).TryGetValue("build_metadata.AdditionalFiles.Address", out var address);
                    files.Add(file, address);
                }

            }

            foreach (var address in addresses)
            {
                success = false;
                string name = "";
                StringBuilder code = new StringBuilder();

                try
                {
                    var p = Process.Start(new ProcessStartInfo("dotnet", "anchorgen -a " + address + " -s") { RedirectStandardOutput = true, UseShellExecute = false, CreateNoWindow = true, WindowStyle = ProcessWindowStyle.Hidden });


                    p.OutputDataReceived += (sender, args) =>
                    {
                        if (string.IsNullOrEmpty(name)) name = args.Data;
                        else code.AppendLine(args.Data);
                    };

                    p.BeginOutputReadLine();
                    p.WaitForExit();

                    if (p.ExitCode == 0) success = true;
                }
                catch (Exception)
                { }

                if (success)
                {
                    context.AddSource(name + ".g.cs", code.ToString());

                    context.ReportDiagnostic(Diagnostic.Create(new DiagnosticDescriptor("ANCHOR-GENERATOR-02", "Success", "Successfully generated code for program '" + name + "'", "CodeGeneration", DiagnosticSeverity.Info, true), null));
                }
                else
                {
                    context.ReportDiagnostic(Diagnostic.Create(new DiagnosticDescriptor("ANCHOR-GENERATOR-04", "Failure", "Unable to Generate code from address " + address, "CodeGeneration", DiagnosticSeverity.Warning, true), null));
                }
            }



            foreach (var file in files)
            {
                success = false;
                string name = "";
                StringBuilder code = new StringBuilder();

                try
                {
                    var parameters = "anchorgen -i " + file.Key.Path + " -s";

                    if (!string.IsNullOrEmpty(file.Value)) parameters += $" -a {file.Value}";
                    var p = Process.Start(new ProcessStartInfo("dotnet", parameters) { RedirectStandardOutput = true, UseShellExecute = false, CreateNoWindow = true, WindowStyle = ProcessWindowStyle.Hidden });


                    p.OutputDataReceived += (sender, args) =>
                    {
                        if (string.IsNullOrEmpty(name)) name = args.Data;
                        else code.AppendLine(args.Data);
                    };

                    p.BeginOutputReadLine();
                    p.WaitForExit();

                    if (p.ExitCode == 0) success = true;
                }
                catch (Exception)
                { }

                if (success)
                {
                    context.AddSource(name + ".g.cs", code.ToString());

                    context.ReportDiagnostic(Diagnostic.Create(new DiagnosticDescriptor("ANCHOR-GENERATOR-02", "Success", "Successfully generated code for program '" + name + "'", "CodeGeneration", DiagnosticSeverity.Info, true), null));
                }
                else
                {
                    context.ReportDiagnostic(Diagnostic.Create(new DiagnosticDescriptor("ANCHOR-GENERATOR-03", "Failure", "Unable to Generate code from file " + file.Key.Path, "CodeGeneration", DiagnosticSeverity.Warning, true), null));
                }
            }

        }

        public void Initialize(GeneratorInitializationContext context)
        {
            //
        }
    }
}
