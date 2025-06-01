using Microsoft.SemanticKernel;

// AI が不要なら Kernel の作成は簡単
var kernel = new Kernel();
// Kernel 作成後にプラグインを登録することもできる
kernel.Plugins.AddFromFunctions("TimePlugin",
    [
        KernelFunctionFactory.CreateFromMethod(
            () => TimeProvider.System.GetLocalNow(),
            functionName: "GetLocalNow",
            description: "現在のローカル時間を取得します。"),
    ]);

// プラグインの関数を呼び出す
var result = await kernel.InvokeAsync("TimePlugin", "GetLocalNow");
Console.WriteLine(result.GetValue<DateTimeOffset>());
