// ReSharper disable InconsistentNaming
namespace MathCore.SberGPT;

public enum RequestRole
{
    /// <summary>Системный промпт, который задает роль модели, например, должна модель отвечать как академик или как школьник</summary>
    user,
    /// <summary>Ответ модели</summary>
    system,
    /// <summary>Сообщение пользователя</summary>
    assistant,
    /// <summary>
    /// Сообщение с результатом работы пользовательской функции.<br/>
    /// В сообщении с этой ролью передавайте в поле content валидный JSON-объект с результатами работы функции
    /// </summary>
    function
}