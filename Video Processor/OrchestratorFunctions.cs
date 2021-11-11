using System;
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

        var videoLocation = context.GetInput<string>();

        string transcodedLocation = null;
        string thumbnailLocation = null;
        string withIntroLocation = null;

        try
        {
            // calling first function:
            logger.LogInformation("about to call transcode video activity");
            transcodedLocation = await context.CallActivityAsync<string>("TranscodeVideo", videoLocation);

            // calling second function:
            logger.LogInformation("about to call extract thumbnail activity");
            thumbnailLocation = await context.CallActivityAsync<string>("ExtractThumbnail", transcodedLocation);

            // and the final one:
            logger.LogInformation("about to call prepend intro activity");
            withIntroLocation = await context.CallActivityAsync<string>("PrependIntro", thumbnailLocation);
        }
        catch (Exception e)
        {
            logger.LogError($"Caught an error from an activity: {e.Message}");
            await context.CallActivityAsync<string>("Cleanup",
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
            WithIntro = withIntroLocation
        };
    }
}