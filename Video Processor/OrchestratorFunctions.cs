using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;

namespace VideoProcessor;

public class OrchestratorFunctions
{
    [FunctionName(nameof(ProcessVideoOrchestrator))]
    public static async Task<object> ProcessVideoOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context, ILogger logger)
    {
        logger = context.CreateReplaySafeLogger(logger);
        
        string transcodedLocation = null;
        string thumbnailLocation = null;
        string withIntroLocation = null;
        var approvalResult = "Unknown";

        var videoLocation = context.GetInput<string>();

        try
        {
            var transcodeResults =
                await context.CallSubOrchestratorAsync<VideoFileInfo[]>(nameof(TranscodeVideoOrhcestrator),
                    videoLocation);

            transcodedLocation = transcodeResults.OrderByDescending(r => r.BitRate)
                .Select(r => r.Location)
                .First();

            // calling second function:
            logger.LogInformation("about to call extract thumbnail activity");
            thumbnailLocation = await context.CallActivityAsync<string>(nameof(ActivityFunctions.ExtractThumbnail), transcodedLocation);

            // and the final one:
            logger.LogInformation("about to call prepend intro activity");
            withIntroLocation = await context.CallActivityAsync<string>(nameof(ActivityFunctions.PrependIntro), thumbnailLocation);

            await context.CallActivityAsync("SendApprovalRequestEmail", new ApprovalInfo
            {
                OrchestrationId = context.InstanceId,
                VideoLocation = withIntroLocation
            });

            try
            {
                approvalResult =await context.CallActivityAsync<string>(nameof(ActivityFunctions.SendApprovalRequestEmail), TimeSpan.FromSeconds(30));
            }
            catch (TimeoutException )
            {
                logger.LogWarning("Timed out waiting for approval");
                approvalResult = "Timed Out";
            }

            if (approvalResult == "Approved")
            {
                await context.CallActivityAsync(nameof(ActivityFunctions.PublishVideo), withIntroLocation);
            }
            else
            {
                await context.CallActivityAsync(nameof(ActivityFunctions.RejectVideo), withIntroLocation);
            }
        }
        catch (Exception e)
        {
            logger.LogError($"Caught an error from an activity: {e.Message}");
            await context.CallActivityAsync<string>(nameof(ActivityFunctions.Cleanup),
                new[] {transcodedLocation, thumbnailLocation, withIntroLocation});

            return new
            {
                Error = "Failed to process uploaded video", e.Message
            };
        }

        return new
        {
            Transcoded = transcodedLocation,
            Thumbnail = thumbnailLocation,
            WithIntro = withIntroLocation,
            ApprovalResult = approvalResult
        };
    }

    [FunctionName(nameof(TranscodeVideoOrhcestrator))]
    public static async Task<VideoFileInfo[]> TranscodeVideoOrhcestrator(
        [OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        var videoLocation = context.GetInput<string>();
        var bitRates = await context.CallActivityAsync<int[]>(nameof(ActivityFunctions.GetTranscodeBitRates), null);
        var transcodeTasks = bitRates
            .Select(bitRate => new VideoFileInfo {Location = videoLocation, BitRate = bitRate})
            .Select(info => context.CallActivityAsync<VideoFileInfo>(nameof(ActivityFunctions.TranscodeVideo), info))
            .ToList();

        var transcodeResults = await Task.WhenAll(transcodeTasks);
        return transcodeResults;
    }

    [FunctionName(nameof(PeriodicTaskOrchestrator))]
    public static async Task<int> PeriodicTaskOrchestrator(
        [OrchestrationTrigger] IDurableOrchestrationContext context, ILogger logger)
    {
        logger = context.CreateReplaySafeLogger(logger);

        var timesRun = context.GetInput<int>();
        timesRun++;

        logger.LogInformation($"Starting the PeriodicTask activity {context.InstanceId}, {timesRun}");

        await context.CallActivityAsync(nameof(ActivityFunctions.PeriodicActivity), timesRun);

        var nextRun = context.CurrentUtcDateTime.AddSeconds(3);
        await context.CreateTimer(nextRun, CancellationToken.None);

        context.ContinueAsNew(timesRun);

        return timesRun;
    }
}