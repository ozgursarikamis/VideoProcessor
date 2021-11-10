using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;

namespace VideoProcessor;

public class OrchestratorFunctions
{
    [FunctionName("Function1")]
    public static async Task<List<string>> RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        var outputs = new List<string>
        {
            // Replace "hello" with the name of your Durable Activity Function.
            await context.CallActivityAsync<string>("Function1_Hello", "Tokyo"),
            await context.CallActivityAsync<string>("Function1_Hello", "Seattle"),
            await context.CallActivityAsync<string>("Function1_Hello", "London")
        };

        // returns ["Hello Tokyo!", "Hello Seattle!", "Hello London!"]
        return outputs;
    }
}