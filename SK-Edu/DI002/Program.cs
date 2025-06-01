using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;

var services = new ServiceCollection();

// プラグインで使う TimeProvider を登録
services.AddSingleton(TimeProvider.System);

// KernelPluginFactory を使ってプラグインを登録
services.AddSingleton(sp =>
    KernelPluginFactory.CreateFromType<TimePlugin>("TimePlugin", sp));
services.AddSingleton(sp =>
    KernelPluginFactory.CreateFromType<WeatherForecastPlugin>("WeatherForecastPlugin", sp));

// DI コンテナに Kernel を登録
services.AddKernel();

var sp = services.BuildServiceProvider();

// DI コンテナから Kernel を作成
var kernel = sp.GetRequiredService<Kernel>();

// プラグインの関数を呼び出す
var now = await kernel.InvokeAsync("TimePlugin", "GetLocalNow");
var arguments = new KernelArguments
{
    ["location"] = "Tokyo",
};
var weatherForecast = await kernel.InvokeAsync("WeatherForecastPlugin", "GetWeatherForecast", arguments);
var weatherAdvice = await kernel.InvokeAsync("WeatherForecastPlugin", "GetWeatherAdvice", arguments);

// 結果を出力
Console.WriteLine(now.GetValue<DateTimeOffset>());
Console.WriteLine(weatherForecast.GetValue<string>());
Console.WriteLine(weatherAdvice.GetValue<string>());

// Kernel に登録するプラグイン
class TimePlugin(TimeProvider timerProvider)
{
    [KernelFunction]
    public DateTimeOffset GetLocalNow() => timerProvider.GetLocalNow();
}

class WeatherForecastPlugin(TimeProvider timerProvider)
{
    [KernelFunction]
    public string GetWeatherForecast(string location)
    {
        var now = timerProvider.GetLocalNow();
        return $"[{now:yyyy-MM-dd HH:mm}] The weather in {location} is sunny.";
    }

    [KernelFunction]
    public string GetWeatherAdvice(string location)
    {
        var now = timerProvider.GetLocalNow();
        return $"[{now:yyyy-MM-dd HH:mm}] It's a great day to be outside in {location}!";
    }
}
