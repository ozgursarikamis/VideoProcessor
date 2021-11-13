using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;

namespace VideoProcessor
{
    public static class ActivityFunctions
    { 
        [FunctionName(nameof(TranscodeVideo))]
        public static async Task<VideoFileInfo> TranscodeVideo([ActivityTrigger] VideoFileInfo inputVideo, ILogger log)
        {
            log.LogInformation($"Transcoding {inputVideo.Location} to {inputVideo.BitRate}");

            await Task.Delay(3000);
            var transcodedLocation = $"{Path.GetFileNameWithoutExtension(inputVideo.Location)}-{inputVideo.BitRate}kbps.mp4";

            return new VideoFileInfo
            {
                Location = transcodedLocation,
                BitRate = inputVideo.BitRate,
            };
        }

        [FunctionName(nameof(ExtractThumbnail))]
        public static async Task<string> ExtractThumbnail([ActivityTrigger] string inputVideo, ILogger log)
        {
            log.LogInformation($"Extracting Thumbnail {inputVideo}");

            if (inputVideo.Contains("error"))
            {
                throw new InvalidOperationException("Failed to extract thumbnail");
            }
            await Task.Delay(3000);

            return $"{Path.GetFileNameWithoutExtension(inputVideo)}-thumbnail.png";
        }

        [FunctionName(nameof(PrependIntro))]
        public static async Task<string> PrependIntro([ActivityTrigger] string inputVideo, ILogger log)
        {
            var introLocation = Environment.GetEnvironmentVariable("IntroLocation");
            log.LogInformation($"Prepending Intro {introLocation} to {inputVideo}");

            await Task.Delay(3000);

            return $"{Path.GetFileNameWithoutExtension(inputVideo)}-withIntro.mp4";
        }

        [FunctionName(nameof(Cleanup))]
        public static async Task<string> Cleanup([ActivityTrigger] string[] filesToCleanup, ILogger log)
        {
            foreach (var file in filesToCleanup)
            {
                log.LogInformation($"Deleting {file}");
                await Task.Delay(3000);
            }

            return "Cleaned up successfully";
        }
    }
}