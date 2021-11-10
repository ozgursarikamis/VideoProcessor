using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;

namespace VideoProcessor
{
    public static class ActivityFunctions
    {
        [FunctionName(nameof(GetTranscodeBitrates))]
        public static int[] GetTranscodeBitrates([ActivityTrigger] object input)
        {
            return Environment.GetEnvironmentVariable("TranscodeBitrates")
                    .Split(',')
                    .Select(int.Parse)
                    .ToArray();
        }

        [FunctionName(nameof(TranscodeVideo))]
        public static async Task<VideoFileInfo> TranscodeVideo([ActivityTrigger] VideoFileInfo inputVideo, ILogger log)
        {
            log.LogInformation($"Transcoding {inputVideo.Location} to {inputVideo.BitRate}.");
            // simulate doing the activity
            await Task.Delay(5000);
            var transcodedLocation = $"{Path.GetFileNameWithoutExtension(inputVideo.Location)}-{inputVideo.BitRate}kbps.mp4";
            return new VideoFileInfo
            {
                Location = transcodedLocation,
                BitRate = inputVideo.BitRate
            };
        }

        [FunctionName(nameof(ExtractThumbnail))]
        public static async Task<string> ExtractThumbnail([ActivityTrigger] string inputVideo, ILogger log)
        {
            log.LogInformation($"Extracting Thumbnail {inputVideo}.");
            if (inputVideo.Contains("error"))
            {
                throw new InvalidOperationException("Failed to extract thumbnail");
            }

            // simulate doing the activity
            await Task.Delay(5000);
            return $"{Path.GetFileNameWithoutExtension(inputVideo)}-thumbnail.png";
        }

        [FunctionName(nameof(PrependIntro))]
        public static async Task<string> PrependIntro([ActivityTrigger] string inputVideo, ILogger log)
        {
            var introLocation = Environment.GetEnvironmentVariable("IntroLocation");
            log.LogInformation($"Prepending Intro {introLocation} to  {inputVideo}.");

            // simulate doing the activity
            await Task.Delay(5000);
            return $"{Path.GetFileNameWithoutExtension(inputVideo)}-withintro.mp4";
        }

        [FunctionName(nameof(Cleanup))]
        public static async Task<string> Cleanup([ActivityTrigger] string[] filesToCleanUp, ILogger log)
        {
            foreach(var file in filesToCleanUp.Where(f => f != null))
            {
                log.LogInformation($"Deleting {file}");

                // simulate doing the activity
                await Task.Delay(1000);
            }
            return "Cleaned up successfully";
        }
    }

}