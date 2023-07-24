namespace PatternsGenerator.Decorator;

internal class DecoratorAttribute
{
    public const string AttributeFullName = "PatternsGenerator.Decorator.DecoratorAttribute";

    public const string SourceCode = @"
namespace PatternsGenerator.Decorator;

[System.AttributeUsage(System.AttributeTargets.Class)]
public class DecoratorAttribute : System.Attribute
{
}";
}
