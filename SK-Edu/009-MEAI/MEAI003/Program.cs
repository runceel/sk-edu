using System.Text.Json;
using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel;

// AIFunction を作成
var aiFunction = AIFunctionFactory.Create((string format) =>
{
    return TimeProvider.System.GetLocalNow().ToString(format);
});

#pragma warning disable SKEXP0001 
// AIFunction から KernelFunction に変換
// プレビュー機能なので警告の抑制が必要
var kernelFunction = aiFunction.AsKernelFunction();
#pragma warning restore SKEXP0001 

// KernelFunction の API を使って関数を呼び出す
FunctionResult result = await kernelFunction.InvokeAsync(
    new Kernel(),
    new KernelArguments
    {
        ["format"] = "yyyy-MM-dd HH:mm:ss zzz",
    });
// デフォルトで戻り値は JsonElement になる点に注意
Console.WriteLine(result.GetValue<JsonElement>());
