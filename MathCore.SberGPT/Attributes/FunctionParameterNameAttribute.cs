namespace MathCore.SberGPT.Attributes;

[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Field)]
public sealed class FunctionParameterNameAttribute(string Name) : Attribute
{
    public FunctionParameterNameAttribute() : this("") { }

    public string Name { get; set; } = Name;
}