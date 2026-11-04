using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

var root = Path.GetFullPath(args[0]);
var files = new List<object>();
foreach (var folder in new[] { "src", "game" })
foreach (var path in Directory.GetFiles(Path.Combine(root, folder), "*.cs", SearchOption.AllDirectories).Order())
{
    var relative = Path.GetRelativePath(root, path).Replace('\\', '/');
    if (relative.Split('/').Any(p => p is "obj" or "bin" or ".godot")) continue;
    var source = File.ReadAllText(path);
    var tree = CSharpSyntaxTree.ParseText(source);
    var syntax = tree.GetRoot();
    string Owner(SyntaxNode n) => string.Join(".", n.Ancestors().OfType<BaseTypeDeclarationSyntax>().Reverse().Select(t => t.Identifier.Text));
    var types = syntax.DescendantNodes().OfType<BaseTypeDeclarationSyntax>().Select(t => new {
        name = t.Identifier.Text, owner = Owner(t), kind = t.Kind().ToString(),
        line = tree.GetLineSpan(t.Span).StartLinePosition.Line + 1,
        signature = t.ToString().Split('{')[0].Split('\n')[0].Trim()
    }).ToArray();
    var members = new List<object>();
    foreach (var node in syntax.DescendantNodes().Where(n => n is BaseMethodDeclarationSyntax or LocalFunctionStatementSyntax or PropertyDeclarationSyntax or IndexerDeclarationSyntax or AccessorDeclarationSyntax))
    {
        string name = node switch {
            MethodDeclarationSyntax x => x.Identifier.Text,
            ConstructorDeclarationSyntax x => x.Identifier.Text,
            DestructorDeclarationSyntax x => "~" + x.Identifier.Text,
            OperatorDeclarationSyntax x => "operator " + x.OperatorToken.Text,
            ConversionOperatorDeclarationSyntax x => "operator " + x.Type,
            LocalFunctionStatementSyntax x => x.Identifier.Text,
            PropertyDeclarationSyntax x => x.Identifier.Text,
            IndexerDeclarationSyntax => "this[]",
            AccessorDeclarationSyntax x => ((x.Parent?.Parent as PropertyDeclarationSyntax)?.Identifier.Text ?? "this[]") + "." + x.Keyword.Text,
            _ => node.Kind().ToString()
        };
        var text = node.ToString();
        var signature = node.ChildNodes().FirstOrDefault(n => n is BlockSyntax or ArrowExpressionClauseSyntax or AccessorListSyntax);
        var declaration = signature == null ? text : source[node.Span.Start..signature.Span.Start].Trim();
        members.Add(new {
            name, owner = Owner(node), kind = node.Kind().ToString(),
            line = tree.GetLineSpan(node.Span).StartLinePosition.Line + 1,
            endLine = tree.GetLineSpan(node.Span).EndLinePosition.Line + 1,
            signature = declaration, source = text,
            documentation = node.GetLeadingTrivia().ToFullString().Trim(),
            calls = node.DescendantNodes().OfType<InvocationExpressionSyntax>().Select(x => x.Expression.ToString()).Distinct().ToArray(),
            identifiers = node.DescendantNodes().OfType<IdentifierNameSyntax>().Select(x => x.Identifier.Text).Distinct().ToArray()
        });
    }
    files.Add(new { path = relative, source, types, members });
}
Console.Write(JsonSerializer.Serialize(files));
