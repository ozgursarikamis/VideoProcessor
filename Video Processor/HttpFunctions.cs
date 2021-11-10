using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;

namespace VideoProcessor;

public class HttpFunctions
{
    [FunctionName(nameof(ProcessorVideoStarter))]
    public static async Task<IActionResult> ProcessorVideoStarter(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequest req,
        [DurableClient] IDurableOrchestrationClient starter)
    {
        var video = req.GetQueryParameterDictionary()["video"];
        if (video == null)
        {
            return new BadRequestObjectResult("Please pass the video location the query string");
        }

        var instanceId = await starter.StartNewAsync("ProcessVideoOrchestrator", null, video);
        return starter.CreateCheckStatusResponse(req, instanceId);
    }
}