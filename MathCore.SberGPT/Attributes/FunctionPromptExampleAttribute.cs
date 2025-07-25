namespace MathCore.SberGPT.Attributes;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Parameter, AllowMultiple = true)]
public sealed class FunctionPromptExampleAttribute(string Prompt, params string[] ExampleParameter) : Attribute
{
    public FunctionPromptExampleAttribute() : this("") { }

    public string Prompt { get; set; } = Prompt;

    public string[] ExampleParameter { get; set; } = ExampleParameter;
}