namespace MathCore.SberGPT.ConsoleTest;

internal static class Test
{
    public static async Task RunAsync()
    {
        var client = new HttpClient();
        var request = new HttpRequestMessage(HttpMethod.Post, "https://gigachat.devices.sberbank.ru/api/v1/functions/validate");
        request.Headers.Add("X-Request-ID", "79e41a5f-f180-4c7a-b2d9-393086ae20a1");
        request.Headers.Add("X-Session-ID", "b6874da0-bf06-410b-a150-fd5f9164a0b2");
        request.Headers.Add("X-Client-ID", "b6874da0-bf06-410b-a150-fd5f9164a0b2");
        const string token = "eyJjdHkiOiJqd3QiLCJlbmMiOiJBMjU2Q0JDLUhTNTEyIiwiYWxnIjoiUlNBLU9BRVAtMjU2In0.FidEJYuT2p0fAKwDsQVFhw47YEYrdtH2ROTqSp0Vm3kVizByuVYsVYqZ1WCF9qfRcHZlLj3cnUeKLptbiARBeXt3iHx0KTdSSkM-Np_Dktj1q5eHuhth4kAYAGCfPOFbHoMQg2B2uHxGOdW7aOOHtAODWRLlBgUpkjqM2QNENcfof7DxgiwT6_hZvS1CjQS7gqdsT74JrwgMaVtFuetmeV00oWvlX4SWWbIxXBdRjmg0wBPGNGRuhwbg7jo0tzZpt9oEpxqZz5fjOq1-W415mIYZF0kRatuZZCRDQXEKq9mvHeBvGtx9Y9q6YCO_8i2qYDOOgt0wBr5fzzekbKYwPA.IPKMh8KyNLihlfe2XNub4Q.9M28O4loIp0Zw9sybzMjbX2ES2s-dQ-ES1RKzL68pFyHCcKct9WDbnSv2XIaCTDYOVzTmEBV4R7_Azux8tkkM6jEDfzMAfhjwsTRGVxUMlY72c9koQVOFnS1zGMYbXA6u7Giw5Cs5Vp7qvyd2L21z1V_4fxBAfcI7TYMf_9WYy7ReojGG2RBf_zLspDbUao8iWbCmz_N3mS5UKFtwK8wDGx818u5Hqg6vtz-nroDp4LCRePKou-4Tl-Mec6ohl4Nh16nJDkMvb0T1RQ6wmyT8LmSBiu1uJT_X2Qhe9tA0Rx8VlYb6uicPARqZZ6WtZ-3F2yNDISg9_k9KzL15IhdE-Tsj1oBESAMByFnxvTclyLYz1xObAymybxXaUzd_ggFY-g-IgpparB2tw6MRYjrCHqvyy-r8E5hlWBy6Oa3lgVTdxDxLwGdj0JG0gIiD_nAMuF3Do47HhHzerzSkwg9URQSyJ5JUfqcQT6Y4cVu8QuMVzXn8D08WhHpzNn4mXvbpLPP-9z1Kvqnrkaaa4Aob9ujdhnb05ESU_u0aU1SyyGtG9yZZ19wpQJSI1aje0Mx-2LEWo7H6hYEamsUM7LDHzbGd4g-Vwn-WGfTO73sjmDEbA3RhZFyvQBsFaCVwsjI7EcOqFuIWMw51UqlTqwDN4MczWD5pE8X6lTQgdhKORa7IYDBpQxsoqNY7TwA0CwY8Y2q6LiTu9agpe0MXR6afVzQdevkGUst5RcVf6IzN4Q.7SjQcloHABUC4vWSmti_YrviM-lJuWRo800U12VQH6U";
        request.Headers.Add("Authorization", $"Bearer {token}");
        var content = new StringContent("{\n    \"name\": \"weather_forecast\",\n    \"description\": \"Возвращает температуру на заданный период\",\n    \"return_parameters\": {},\n    \"few_shot_examples\": [\n        {\n            \"request\": \"Погода в манжероке на десять дней\",\n            \"params\": {}\n        }\n    ],\n    \"parameters\": {\n        \"type\": \"object\",\n        \"properties\": {\n            \"location\": {\n                \"type\": \"string\",\n                \"description\": \"Местоположение, например, название города\"\n            },\n            \"format\": {\n                \"type\": \"string\",\n                \"enum\": [\n                    \"celsius\",\n                    \"fahrenheit\"\n                ],\n                \"description\": \"Единицы измерения температуры\"\n            },\n            \"num_days\": {\n                \"type\": \"integer\",\n                \"description\": \"Период, для которого нужно вернуть\"\n            }\n        },\n        \"required\": [\n            \"location\",\n            \"format\"\n        ]\n    }\n}", null, "application/json");
        content.Headers.ContentType = new("application/json");
        request.Content = content;
        var response = await client.SendAsync(request);
        //response.EnsureSuccessStatusCode();
        var read_as_string_async = await response.Content.ReadAsStringAsync();
        Console.WriteLine(read_as_string_async);

    }
}
