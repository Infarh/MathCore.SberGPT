namespace MathCore.SberGPT.Attributes;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Parameter | AttributeTargets.Field | AttributeTargets.Property)]
public class GPTAttribute(string Name, string? Description = null) : Attribute
{
    public string Name { get; init; } = Name;

    public string? Description { get; init; } = Description;
}