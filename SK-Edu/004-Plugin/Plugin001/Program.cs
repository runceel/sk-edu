using System.ComponentModel;
using Microsoft.SemanticKernel;

// KernelFunctionFactory を使用して関数を作成
// システムの現在のローカル時間を取得する関数
var getLocalNow = KernelFunctionFactory.CreateFromMethod(
    TimeProvider.System.GetLocalNow,
    new KernelFunctionFromMethodOptions
    {
        Description = "Get the current local time.",
        FunctionName = "GetLocalNow",
        ReturnParameter = new KernelReturnParameterMetadata
        {
            Description = "The current local time as a DateTimeOffset object.",
            ParameterType = typeof(DateTimeOffset),
        }
    });
// 日時をフォーマットする関数
var format = KernelFunctionFactory.CreateFromMethod((DateTimeOffset dateTime, string format) => dateTime.ToString(format),
    new KernelFunctionFromMethodOptions
    {
        Description = "Format a DateTimeOffset object to a string.",
        FunctionName = "FormatDateTime",
        Parameters = [
            new KernelParameterMetadata("dateTime")
            {
                Description = "The DateTimeOffset object to format.",
                ParameterType = typeof(DateTimeOffset),
            },
            new KernelParameterMetadata("format")
            {
                Description = "The format string to use for formatting.",
                ParameterType = typeof(string),
            }
        ],
        ReturnParameter = new KernelReturnParameterMetadata
        {
            Description = "The formatted date and time as a string.",
            ParameterType = typeof(string),
        }
    });

// 関数をまとめてプラグイン化
var timePlugin = KernelPluginFactory.CreateFromFunctions(
    pluginName: "TimePlugin", 
    description: "A plugin that provides time-related functions.",
    functions: [getLocalNow, format]);

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
