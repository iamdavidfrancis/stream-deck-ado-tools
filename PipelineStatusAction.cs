using StreamDeckAzureDevOps.Services;
using StreamDeckAzureDevOps.Models;
using StreamDeckLib;
using StreamDeckLib.Messages;
using System.Threading.Tasks;
using System.Timers;
using StreamDeckAzureDevOps.Services.ResponseModels;

namespace StreamDeckAzureDevOps
{
    [ActionUuid(Uuid = "com.iamdavidfrancis.streamdeckado.pipelinestatus")]
    public class PipelineStatusAction : BaseStreamDeckActionWithSettingsModel<AdoPipelineSettingsModel>
    {
        private AzureDevOpsService azureDevOpsService;
        private Timer timer;

        public override async Task OnKeyUp(StreamDeckEventPayload args)
        {
            await FetchLatestInfo(args);
        }

        public override async Task OnDidReceiveSettings(StreamDeckEventPayload args)
        {
            await base.OnDidReceiveSettings(args);
            await FetchLatestInfo(args);
        }

        public override async Task OnWillAppear(StreamDeckEventPayload args)
        {
            this.azureDevOpsService = new AzureDevOpsService(async (message) =>
            {

                // This is only used for debugging. No-op in release builds.
#if DEBUG
                await Manager.LogMessageAsync(args.context, message);
#else
                await Task.CompletedTask;
#endif
            });
            await base.OnWillAppear(args);

            timer = new Timer(5 * 60 * 1000);
            timer.Elapsed += async (sender, e) =>
            {
                await Manager.ShowOkAsync(args.context);
                await FetchLatestInfo(args);
            };
            timer.Start();

            await FetchLatestInfo(args);
        }

        public override Task OnWillDisappear(StreamDeckEventPayload args)
        {
            if (timer != null)
            {
                timer.Stop();
            }

            return base.OnWillDisappear(args);
        }

        private async Task FetchLatestInfo(StreamDeckEventPayload args)
        {
            var resp = await this.azureDevOpsService.GetReleaseStageInformation(SettingsModel);

            if (resp != null)
            {
                switch (resp.DeploymentStatus)
                {
                    case ReleaseEnvironmentStatus.Succeeded:
                        await Manager.SetImageAsync(args.context, "images/success@2x.png");
                        break;
                    case ReleaseEnvironmentStatus.Failed:
                        await Manager.SetImageAsync(args.context, "images/fail@2x.png");
                        break;
                    case ReleaseEnvironmentStatus.InProgress:
                        await Manager.SetImageAsync(args.context, "images/inProgress@2x.png");
                        break;
                    case ReleaseEnvironmentStatus.PartiallySucceeded:
                        await Manager.SetImageAsync(args.context, "images/partial@2x.png");
                        break;
                    default:
                        await Manager.SetImageAsync(args.context, "images/actionDefaultImage@2x.png");
                        break;
                }
            }
            else
            {
                await Manager.SetImageAsync(args.context, "images/actionDefaultImage@2x.png");
                await Manager.ShowAlertAsync(args.context);
            }
        }
    }
}
