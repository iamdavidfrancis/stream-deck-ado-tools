using StreamDeckAzureDevOps.Services;
using StreamDeckAzureDevOps.Models;
using StreamDeckLib;
using StreamDeckLib.Messages;
using System.Threading.Tasks;
using System.Timers;

namespace StreamDeckAzureDevOps
{
    [ActionUuid(Uuid = "com.iamdavidfrancis.streamdeckado.buildpipelinestatus")]
    public class BuildPipelineStatusAction : BaseStreamDeckActionWithSettingsModel<AdoBuildSettingsModel>
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
            var resp = await this.azureDevOpsService.GetBuildStatusInformation(SettingsModel);
            await Manager.SetImageAsync(args.context, resp);

#if DEBUG
            await Manager.LogMessageAsync(args.context, resp);
#endif
        }
    }
}
