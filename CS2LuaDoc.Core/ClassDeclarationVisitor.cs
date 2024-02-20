using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CS2LuaDoc.Core;

public class ClassDeclarationVisitor : SymbolVisitor
{
    public override void VisitNamespace(INamespaceSymbol symbol)
    {
        foreach (var member in symbol.GetMembers())
        {
            member.Accept(this);
        }
    }

    public override void VisitNamedType(INamedTypeSymbol symbol)
    {
        classDeclarations.Add(symbol);
        foreach (var member in symbol.GetTypeMembers())
        {
            base.Visit(member);
        }
    }

    public List<INamedTypeSymbol> classDeclarations { get; } = new();
}