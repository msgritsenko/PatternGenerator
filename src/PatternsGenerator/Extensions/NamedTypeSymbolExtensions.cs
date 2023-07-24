using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Linq;

namespace PatternsGenerator.Extensions;

internal static class NamedTypeSymbolExtensions
{
    public static string GetFullNamespace(this INamedTypeSymbol symbol)
    {
        string result = symbol.ContainingNamespace.Name;

        var symbolNamespace = symbol.ContainingNamespace.ContainingNamespace;
        while (symbolNamespace != null && !symbolNamespace.IsGlobalNamespace)
        {
            result = symbolNamespace.Name + "." + result;
            symbolNamespace = symbolNamespace.ContainingNamespace;
        }

        return result;
    }

    public static string GetAccessibility(this INamedTypeSymbol symbol)
    {
        return symbol.DeclaredAccessibility switch
        {
            Accessibility.Public => "public",
            Accessibility.Internal => "internal",
            Accessibility.Private => "private",
            _ => "private"
        };
    }

    public static List<ISymbol> GetInterfaceMembers(this INamedTypeSymbol symbol)
    {
        var ancestorInterfaces = symbol.AllInterfaces;
        var ancestorMembers = ancestorInterfaces.SelectMany(a => a.GetMembers());
        var members = symbol.GetMembers().Concat(ancestorMembers);

        return members.ToList();
    }
}
