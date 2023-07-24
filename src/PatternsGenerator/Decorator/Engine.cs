using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using PatternsGenerator.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;

namespace PatternsGenerator.Decorator;

internal readonly struct DecoratorClassDto
{
    public readonly string Name;
    public readonly INamedTypeSymbol Symbol;
    public readonly IFieldSymbol Field;
    public readonly List<IMethodSymbol> AbsentMembers;

    public DecoratorClassDto(string name, INamedTypeSymbol symbol, IFieldSymbol field, List<IMethodSymbol> absentMembers)
    {
        Name = name;
        Symbol = symbol;
        Field = field;
        AbsentMembers = absentMembers;
    }
}

internal static class Engine
{
    public static void Execute(Compilation compilation, ImmutableArray<ClassDeclarationSyntax> declarations, SourceProductionContext context)
    {
        if (declarations.IsDefaultOrEmpty)
        {
            return;
        }

        IEnumerable<ClassDeclarationSyntax> distinctEnums = declarations.Distinct();

        IReadOnlyCollection<DecoratorClassDto> decoratorClasses = GetTypesToGenerate(compilation, distinctEnums, context.CancellationToken);

        if (decoratorClasses.Count > 0)
        {
            foreach (var classDto in decoratorClasses)
            {
                // generate the source code and add it to the output
                string result = GenerateExtensionClass(classDto);
                context.AddSource($"{classDto.Name}.g.cs", SourceText.From(result, Encoding.UTF8));
            }
        }
    }

    static IReadOnlyCollection<DecoratorClassDto> GetTypesToGenerate(Compilation compilation, IEnumerable<ClassDeclarationSyntax> classes, CancellationToken ct)
    {
        INamedTypeSymbol enumAttribute = compilation.GetTypeByMetadataName(DecoratorAttribute.AttributeFullName);

        if (enumAttribute == null)
        {
            return Array.Empty<DecoratorClassDto>();
        }

        var result = new List<DecoratorClassDto>();

        foreach (ClassDeclarationSyntax enumDeclarationSyntax in classes)
        {
            //Debugger.Launch();

            ct.ThrowIfCancellationRequested();

            SemanticModel semanticModel = compilation.GetSemanticModel(enumDeclarationSyntax.SyntaxTree);
            if (semanticModel.GetDeclaredSymbol(enumDeclarationSyntax) is not INamedTypeSymbol classSymbol)
            {
                continue;
            }

            if (classSymbol.AllInterfaces.IsDefaultOrEmpty)
            {
                continue;
            }

            var decoratorField = classSymbol.GetMembers()
                .OfType<IFieldSymbol>()
                .Where(t => t.Type.TypeKind == TypeKind.Interface)
                .FirstOrDefault(t => classSymbol.AllInterfaces.FirstOrDefault(i => SymbolEqualityComparer.Default.Equals(i, t.Type)) != null);

            if (decoratorField == null)
            {
                continue;
            }

            // есть ли нереализованные методы интерфейса
            List<IMethodSymbol> absentMembers = (decoratorField.Type as INamedTypeSymbol)
                .GetInterfaceMembers()
                .OfType<IMethodSymbol>()
                .Where(m => !(m.AssociatedSymbol is IPropertySymbol))
                .Where(m => classSymbol.FindImplementationForInterfaceMember(m) == null)
                .ToList();

            if (absentMembers.Count > 0)
            {
                result.Add(new DecoratorClassDto(classSymbol.Name, classSymbol, decoratorField, absentMembers));
            }
        }

        return result;
    }

    public static string GenerateExtensionClass(DecoratorClassDto dto)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"namespace {dto.Symbol.GetFullNamespace()};");
        sb.AppendLine();
        sb.AppendLine($"{dto.Symbol.GetAccessibility()} partial class {dto.Name}");
        sb.AppendLine("{");
        foreach (var member in dto.AbsentMembers)
        {
            var typeParametersStrings = member.TypeParameters.Select(t => t.ToDisplayString());
            var parametersStrings = member.Parameters.Select(p => $@"{p.Type} {p.Name}");
            var formattedAccessibility = (member.ReturnType.DeclaredAccessibility != Accessibility.NotApplicable ? member.ReturnType.DeclaredAccessibility : Accessibility.Public).ToString().ToLower();
            var signature = $@"{formattedAccessibility} {member.ReturnType} {member.Name}{(member.IsGenericMethod ? $@"<{string.Join(", ", typeParametersStrings)}>" : string.Empty)}({string.Join(", ", parametersStrings)})";
            var callParameters = $@"{string.Join(", ", member.Parameters.Select(p => p.Name))}";

            var call = $@"{dto.Field.Name}.{member.Name}{(member.IsGenericMethod ? $@"<{string.Join(", ", typeParametersStrings)}>" : string.Empty)}({callParameters})";

            sb.AppendLine(FormatDisplayMethods(signature, call, member.ReturnType));
            sb.AppendLine();
        }
        sb.AppendLine("}");
        sb.AppendLine();

        return sb.ToString();
    }

    private static string FormatDisplayMethods(string signature, string call, ITypeSymbol returnType)
    {
        return
$@"    {signature} 
    {{
        {(returnType.Name == "Void" ? string.Empty : "return ")}{call};
    }}";
    }
}
