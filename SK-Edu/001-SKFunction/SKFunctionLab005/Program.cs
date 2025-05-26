using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;

KernelFunction f = KernelFunctionFactory.CreateFromMethod(
    // Kernel と TimeProvider と string を引数に受け取る
    (Kernel kernel, [FromKernelServices]TimeProvider timeProvider, string name) =>
        $"Hello, {name} from {kernel} at {timeProvider.GetLocalNow()}!");

// TimeProvider を登録した IServiceProvider を作成
var services = new ServiceCollection()
    .AddSingleton(TimeProvider.System)
    .BuildServiceProvider();

// Kernel を作成するときに IServiceProvider を渡すことができる
var kernel = new Kernel(services);

// KernelArguments では name 引数だけを渡す
var r = await f.InvokeAsync(kernel, new KernelArguments
{
    ["name"] = "Kazuki"
});

// 結果を表示
Console.WriteLine(r.GetValue<string>());