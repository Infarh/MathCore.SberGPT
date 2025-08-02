namespace MathCore.SberGPT.Attributes;

/// <summary>Атрибут для задания имени и описания функции или параметра.</summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Parameter | AttributeTargets.Field | AttributeTargets.Property)]
public class GPTAttribute(string Name, string? Description = null) : Attribute
{
    /// <summary>Имя функции или параметра.</summary>
    public string Name { get; init; } = Name;

    /// <summary>Описание функции или параметра.</summary>
    public string? Description { get; init; } = Description;
}