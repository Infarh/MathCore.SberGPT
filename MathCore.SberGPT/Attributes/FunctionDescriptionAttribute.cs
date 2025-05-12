namespace MathCore.SberGPT.Attributes;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Parameter)]
public sealed class FunctionDescriptionAttribute(string Description) : Attribute
{
    public FunctionDescriptionAttribute() : this("") { }

    public string Description { get; set; } = Description;
}

