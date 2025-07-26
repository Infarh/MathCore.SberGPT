namespace MathCore.SberGPT.Attributes;

/// <summary>Атрибут для описания элементов массива в JSON-схеме</summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
public sealed class ItemsDescriptionAttribute(string Description) : Attribute
{
    /// <summary>Описание элемента массива</summary>
    public string Description { get; } = Description;
}