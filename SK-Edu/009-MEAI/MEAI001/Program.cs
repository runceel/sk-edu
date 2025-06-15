using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel;

// Kernel がなくても動く関数
var simpleFunction = KernelFunctionFactory.CreateFromMethod(TimeProvider.System.GetLocalNow, "GetLocalNow");
await InvokeAIFunctionAndWriteOutputAsync(simpleFunction);

async Task InvokeAIFunctionAndWriteOutputAsync(AIFunction function)
{
    var result = await function.InvokeAsync();
    Console.WriteLine(result);
}
