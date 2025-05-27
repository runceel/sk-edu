using System.ComponentModel;
using Microsoft.SemanticKernel;

// クラスからプラグインを作成
var timePlugin = KernelPluginFactory.CreateFromType<TimePlugin>();

// 各種メタデータを表示
Console.WriteLine("====================================");
Console.WriteLine($"Plugin Name: {timePlugin.Name}, ({timePlugin.Description})");
foreach (var function in timePlugin)
{
    // 関数のメタデータを表示
    Console.WriteLine($"  Function Name: {function.Name} ({function.Description})");
    foreach (var parameter in function.Metadata.Parameters)
    {
        // パラメーターのメタデータを表示
        Console.WriteLine($"    Parameter: {parameter.Name} ({parameter.Description}) - Type: {parameter.ParameterType}");
    }
    // 戻り値のメタデータを表示
    Console.WriteLine($"    Return: ({function.Metadata.ReturnParameter.Description}) - Type: {function.Metadata.ReturnParameter.ParameterType}");
}
Console.WriteLine("====================================");

// プラグインから関数を取得して呼び出す
if (timePlugin.TryGetFunction("GetLocalNow", out var localNowFunction)
    && timePlugin.TryGetFunction("FormatDateTime", out var formatDateTime))
{
    var localNowResult = await localNowFunction.InvokeAsync();
    Console.WriteLine($"GetLocalNow: {localNowResult}");

    var formattedDateTime = await formatDateTime.InvokeAsync(new KernelArguments
    {
        ["dateTime"] = localNowResult,
        ["format"] = "yyyy-MM-dd HH:mm:ss zzz"
    });
    Console.WriteLine($"Formatted DateTime: {formattedDateTime}");
}

// クラスとして定義
[Description("A plugin that provides time-related functions.")]
class TimePlugin
{
    [KernelFunction, Description("Get the current local time.")]
    [return: Description("The current local time as a DateTimeOffset object.")]
    public DateTimeOffset GetLocalNow() => TimeProvider.System.GetLocalNow();

    [KernelFunction, Description("Format a DateTimeOffset object to a string.")]
    [return: Description("The formatted date and time as a string.")]
    public string FormatDateTime(
        [Description("The DateTimeOffset object to format.")]
        DateTimeOffset dateTime,
        [Description("The format string to use for formatting.")]
        string format) =>
        dateTime.ToString(format);
}
