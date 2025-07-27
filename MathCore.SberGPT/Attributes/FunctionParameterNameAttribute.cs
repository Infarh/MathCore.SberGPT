namespace MathCore.SberGPT.Attributes;

/// <summary>Атрибут для задания имени параметра функции.</summary>
[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Field)]
public sealed class FunctionParameterNameAttribute(string Name) : Attribute
{
    /// <summary>Имя параметра функции.</summary>
    public string Name { get; set; } = Name;
}