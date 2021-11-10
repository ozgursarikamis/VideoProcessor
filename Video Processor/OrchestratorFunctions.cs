using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;

namespace VideoProcessor;

public class OrchestratorFunctions
{
    [FunctionName(nameof(ProcessVideoOrchestrator))]
    public static async Task<object> ProcessVideoOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        var videoLocation = context.GetInput<string>();
        
        // calling first function:
        var transcodedLocation = await context.CallActivityAsync<string>("TranscodeVideo", videoLocation);
        
        // calling second function:
        var thumbnailLocation = await context.CallActivityAsync<string>("ExtractThumbnail", transcodedLocation);

        // and the final one:
        var withIntroLocation = await context.CallActivityAsync<string>("PrependIntro", thumbnailLocation);

        return new
        {
            Transcoded = transcodedLocation,
            Thumbnail = thumbnailLocation,
            WithIntro = withIntroLocation
        };
    }
}