using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace VideoProcessor;

public class HttpFunctions
{
    [FunctionName(nameof(ProcessVideoStarter))]
    public static async Task<IActionResult> ProcessVideoStarter(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")]
        HttpRequest req,
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

    [FunctionName(nameof(SubmitVideoApproval))]
    public static async Task<IActionResult> SubmitVideoApproval(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "SubmitVideoApproval/{id}")]
        HttpRequest req,
        [DurableClient] IDurableOrchestrationClient client,
        [Table("Approvals", "Approval", "{id}", Connection = "AzureWebJobsStorage")]
        Approval approval,
        ILogger log)
    {
        // nb if the approval code doesn't exist, framework just returns a 404 before we get here
        var result = req.GetQueryParameterDictionary()["result"];

        if (result == null)
            return new BadRequestObjectResult("Need an approval result");

        log.LogWarning($"Sending approval result to {approval.OrchestrationId} of {result}");
        // send the ApprovalResult external event to this orchestration
        await client.RaiseEventAsync(approval.OrchestrationId, "ApprovalResult", result);

        return new OkResult();
    }
}