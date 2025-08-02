namespace MathCore.SberGPT.Attributes;

/// <summary>Атрибут для задания имени функции.</summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class FunctionNameAttribute(string Name) : Attribute
{
    /// <summary>Имя функции.</summary>
    public string Name { get; set; } = Name;
}