namespace MathCore.SberGPT.ConsoleTest.Tests;

internal static class DayOfWeekTest
{
    public static DayOfWeek GetDayOfWeek(DayOfWeek Start, int Day, int Month, int Year)
    {
        if (Month is < 1 or > 12)
            throw new ArgumentOutOfRangeException(nameof(Month), Month, "Месяц не может быть меньше 1 и больше 12");
        var is_year_leap = Year % 4 == 0 && (Year % 100 != 0 || Year % 400 == 0);
        if (Day < 1 || Day > 30 + ((Month - 1) % 7 % 2 == 0 ? 1 : Month != 2 ? 0 : is_year_leap ? -1 : -2))
            throw new ArgumentOutOfRangeException(nameof(Day), Day, "Некорректное значение дня");

        var year_day_offset = 0;
        for (var month = 1; month < Month; month++)
            year_day_offset += 30 + ((month - 1) % 7 % 2 == 0 ? 1 : month != 2 ? 0 : is_year_leap ? -1 : -2);

        var start = (int)Start;
        var day_of_year = year_day_offset + Day - 1;
        var day_of_week = (day_of_year + start) % 7;

        var day_value = start + day_of_week;
        var day_result = (DayOfWeek)day_value;
        return day_result;
    }
}
