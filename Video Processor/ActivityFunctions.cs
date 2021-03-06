using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using SendGrid.Helpers.Mail;

namespace VideoProcessor
{
    public static class ActivityFunctions
    {
        [FunctionName(nameof(SendApprovalRequestEmail))]
        public static void SendApprovalRequestEmail(
            [ActivityTrigger] ApprovalInfo approvalInfo,
            [SendGrid(ApiKey = "SendGridKey")] out SendGridMessage message,
            [Table("Approvals", "AzureWebJobsStorage")] out Approval approval,
            ILogger log)
        {
            var approvalCode = Guid.NewGuid().ToString("N");
            approval = new Approval
            {
                PartitionKey = "Approval",
                RowKey = approvalCode,
                OrchestrationId = approvalInfo.OrchestrationId
            };
            var approverEmail = new EmailAddress(Environment.GetEnvironmentVariable("ApproverEmail"));
            var senderEmail = new EmailAddress(Environment.GetEnvironmentVariable("SenderEmail"));

            log.LogInformation($"Sending approval request for {approvalInfo.VideoLocation}");
            var host = Environment.GetEnvironmentVariable("Host");

            var functionAddress = $"{host}/api/SubmitVideoApproval/{approvalCode}";
            var approvedLink = functionAddress + "?result=Approved";
            var rejectedLink = functionAddress + "?result=Rejected";
            var body = $"Please review {approvalInfo.VideoLocation}<br>"
                       + $"<a href=\"{approvedLink}\">Approve</a><br>"
                       + $"<a href=\"{rejectedLink}\">Reject</a>";
            message = new SendGridMessage
            {
                Subject = "A video is awaiting approval (V2)",
                From = senderEmail,
                HtmlContent = body
            };
            message.AddTo(approverEmail);
            log.LogWarning(body);
        }

        [FunctionName(nameof(PublishVideo))]
        public static async Task PublishVideo([ActivityTrigger] string inputVideo, ILogger log)
        {
            log.LogInformation($"Publishing {inputVideo}.");
            await Task.Delay(1000); 
        }

        [FunctionName(nameof(RejectVideo))]
        public static async Task RejectVideo([ActivityTrigger] string inputVideo, ILogger log)
        {
            log.LogInformation($"Rejecting {inputVideo}.");
            await Task.Delay(1000);
        }

        [FunctionName(nameof(GetTranscodeBitRates))]
        public static int[] GetTranscodeBitRates([ActivityTrigger] object input)
        {
            return Environment.GetEnvironmentVariable("TranscodeBitrates")
                ?.Split(',').Select(int.Parse).ToArray();
        }

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

        [FunctionName(nameof(PeriodicActivity))]
        public static void PeriodicActivity([ActivityTrigger] int timesRun, ILogger logger)
        {
            logger.LogWarning($"Running the periodic activity, times run = {timesRun}");
        }
    }
}