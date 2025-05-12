namespace MathCore.SberGPT.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public sealed class FunctionNameAttribute(string Name) : Attribute
{
    public FunctionNameAttribute() : this("") { }

    public string Name { get; set; } = Name;
}