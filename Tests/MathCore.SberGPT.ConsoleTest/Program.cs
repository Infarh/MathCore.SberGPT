using MathCore.SberGPT;
using MathCore.SberGPT.Attributes;
using MathCore.SberGPT.ConsoleTest.HostedServices;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.ComponentModel;
using System.Text.Json;

var builder = Host.CreateApplicationBuilder();

var cfg = builder.Configuration;
cfg.AddJsonFile("appsettings.json", true);
cfg.AddUserSecrets(typeof(Program).Assembly);

var log = builder.Logging;
log.AddConsole();
log.AddDebug();
log.AddFilter((n, l) => (n, l) switch
{
    ("MathCore.SberGPT.GptClient", >= LogLevel.Trace) => true,
    (_, >= LogLevel.Information) => true,
    _ => false
});
log.AddConfiguration(cfg);

var srv = builder.Services;
srv.AddSberGPT();

srv.AddHostedService<MainWorker>();

var app = builder.Build();

// Тест нового метода GetComplexTypeDescription
Console.WriteLine("=== Тестирование расширенного GetTypeDescription ===");

// Создадим делегат для функции с комплексными типами
Func<string, Student[], Dictionary<string, object>, Task<IEnumerable<Student>>> complexFunction = 
    (query, students, metadata) => Task.FromResult(students.AsEnumerable());

using (var scope = app.Services.CreateScope())
{
    var client = scope.ServiceProvider.GetRequiredService<GptClient>();
    
    // Используем рефлексию для доступа к приватному методу (только для демонстрации)
    var methodInfo = typeof(GptClient).GetMethod("GetFunctionInfo", 
        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
    
    if (methodInfo != null)
    {
        var functionInfo = methodInfo.Invoke(null, [complexFunction]);
        var json = JsonSerializer.Serialize(functionInfo, new JsonSerializerOptions 
        { 
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        });
        
        Console.WriteLine("Function Info JSON:");
        Console.WriteLine(json);
    }
}

await app.RunAsync();

Console.WriteLine("End.");
return;

[FunctionName("GetCityWeather")]
[FunctionDescription("Получение погоды в указанном городе в заданных единицах измерения")]
static string GetWeather(
    [Description("Город")] string City,
    [Description("Единицы измерения температуры")] string Unit)
    => $"Погода в {City}: солнечно, 25 deg {Unit}";

internal class StudentGroup
{
    [Description("Уникальный идентификатор группы")]
    public int Id { get; set; }

    [Description("Название группы")]
    public string Name { get; set; } = string.Empty;
}

internal class Student
{
    [Description("Уникальный идентификатор студента")]
    public int Id { get; set; }
    
    [Description("Полное имя студента")]
    public string Name { get; set; } = string.Empty;
    
    [Description("Группа, к которой принадлежит студент")]
    public StudentGroup Group { get; set; } = null!;

    [Description("Средний балл успеваемости")]
    public double Rating { get; set; }

    [Description("Дополнительные метаданные о студенте")]
    public Dictionary<string, string> MetaData { get; set; } = [];
    
    [Description("Дата рождения студента")]
    public DateTime? BirthDate { get; set; }
    
    [Description("Активен ли студент в настоящее время")]
    public bool IsActive { get; set; } = true;
}