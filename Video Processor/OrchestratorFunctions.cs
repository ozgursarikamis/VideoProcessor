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
        var videoLocation = context.GetInput<string>();
        
        // calling first function:
        logger.LogInformation("about to call transcode video activity");
        var transcodedLocation = await context.CallActivityAsync<string>("TranscodeVideo", videoLocation);

        // calling second function:
        logger.LogInformation("about to call extract thumbnail activity");
        var thumbnailLocation = await context.CallActivityAsync<string>("ExtractThumbnail", transcodedLocation);

        // and the final one:
        logger.LogInformation("about to call prepend intro activity");
        var withIntroLocation = await context.CallActivityAsync<string>("PrependIntro", thumbnailLocation);

        return new
        {
            Transcoded = transcodedLocation,
            Thumbnail = thumbnailLocation,
            WithIntro = withIntroLocation
        };
    }
}