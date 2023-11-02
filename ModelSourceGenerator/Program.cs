using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

#pragma warning disable RS1035 // Do not use banned APIs for analyzers

namespace SourceGeneratorSamples
{
    [Generator]
    public class AutoNotifyGenerator : ISourceGenerator
    {

        public void Initialize(GeneratorInitializationContext context)
        {
#if DEBUG
            if (!Debugger.IsAttached)
            {
                Debugger.Launch();
            }
#endif
            Debug.WriteLine("Initalize code generator");
            // Register a syntax receiver that will be created for each generation pass
            context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
        }

        public void Execute(GeneratorExecutionContext context)
        {
            // retrieve the populated receiver
            if (!(context.SyntaxContextReceiver is SyntaxReceiver receiver))
                return;

            INamedTypeSymbol? offsetFieldSymbol = context.Compilation.GetTypeByMetadataName("RszTool.OffsetField`1");

            foreach (var classSymbol in receiver.ModelAutoOffsetFieldClasses)
            {
                string namespaceName = classSymbol.ContainingNamespace.ToDisplayString();
                // begin building the generated source
                StringBuilder source = new StringBuilder($@"
    namespace {namespaceName}
    {{
        public partial class {classSymbol.Name}
        {{
    ");
                foreach (var symbol in classSymbol.GetMembers())
                {
                    if (symbol is IFieldSymbol fieldSymbol)
                    {
                        if (fieldSymbol.Type.OriginalDefinition.Equals(offsetFieldSymbol, SymbolEqualityComparer.Default))
                        {
                            Debug.WriteLine(fieldSymbol.Type);
                            ProcessField(source, fieldSymbol);
                        }
                    }
                }

                source.Append("} }");
                context.AddSource($"{classSymbol.Name}_autoOffsetField.g.cs", SourceText.From(source.ToString(), Encoding.UTF8));
            }
        }

        private void ProcessField(StringBuilder source, IFieldSymbol fieldSymbol)
        {
            // get the name and type of the field
            string fieldName = fieldSymbol.Name;
            ITypeSymbol fieldType = fieldSymbol.Type;
            string fieldTypeString;
            if (fieldType is INamedTypeSymbol namedTypeSymbol && namedTypeSymbol.IsGenericType)
            {
                fieldTypeString = namedTypeSymbol.TypeArguments.First().ToString();
            }
            else
            {
                fieldTypeString = fieldType.ToString();
            }
            source.Append($"\npublic {fieldTypeString} {ToTitleCase(fieldName)} {{ get => {fieldName}.Value; set => {fieldName}.Value = value; }}\n");
        }

        static string ToTitleCase(string text)
        {
            return char.ToUpperInvariant(text[0]) + text.Substring(1);
        }

        /// <summary>
        /// Created on demand before each generation pass
        /// </summary>
        class SyntaxReceiver : ISyntaxContextReceiver
        {
            public List<INamedTypeSymbol> ModelAutoOffsetFieldClasses { get; } = new();

            /// <summary>
            /// Called for every syntax node in the compilation, we can inspect the nodes and save any information useful for generation
            /// </summary>
            public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
            {
                if (context.Node is ClassDeclarationSyntax syntax &&
                    syntax.BaseList != null && syntax.BaseList.Types.Count >= 1 &&
                    syntax.AttributeLists.Count > 0)
                {
                    INamedTypeSymbol? classSymbol = context.SemanticModel.GetDeclaredSymbol(syntax);
                    if (classSymbol != null && classSymbol.GetAttributes().Any(
                        ad => ad.AttributeClass?.ToDisplayString() == "RszTool.ModelAutoOffsetFieldAttribute"))
                    {
                        ModelAutoOffsetFieldClasses.Add(classSymbol);
                    }
                }
            }
        }
    }
}
