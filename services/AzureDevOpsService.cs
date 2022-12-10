using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.TeamFoundation.Build.WebApi;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.ReleaseManagement.WebApi;
using Microsoft.VisualStudio.Services.ReleaseManagement.WebApi.Clients;
using Microsoft.VisualStudio.Services.WebApi;
using StreamDeckAzureDevOps.Models;
using StreamDeckAzureDevOps.Utilities;

using Deployment = Microsoft.VisualStudio.Services.ReleaseManagement.WebApi.Deployment;

namespace StreamDeckAzureDevOps.Services
{
    public class AzureDevOpsService
    {
        public TimeSpan StaleInProgressBuild { get; set; } = TimeSpan.FromDays(1);
        private const string AdoOrganizationUrl = "https://vsrm.dev.azure.com/{0}";
        private readonly Logger logger;

        public delegate Task Logger(string message);

        public AzureDevOpsService(Logger logger)
        {
            this.logger = logger;
        }

        /// <summary>
        /// Gets the build status of the latest run of a build pipeline.
        /// </summary>
        /// <param name="adoPipelineSettingsModel">Settings with information about the pipeline.</param>
        /// <returns></returns>
        public async Task<string> GetBuildStatusInformation(AdoBuildSettingsModel adoPipelineSettingsModel)
        {
            var connection = GetConnection(adoPipelineSettingsModel);
            var buildClient = connection.GetClient<BuildHttpClient>();

            await logger("Fetching builds");
            try
            {
                var latestBuilds = await buildClient.GetBuildsAsync(
                    adoPipelineSettingsModel.Project,
                    definitions: new[] { adoPipelineSettingsModel.DefinitionId },
                    top: 1,
                    queryOrder: BuildQueryOrder.QueueTimeDescending,
                    statusFilter: BuildStatus.InProgress,
                    branchName: adoPipelineSettingsModel.FullBranchName());

                var build = latestBuilds?.FirstOrDefault(x => x.StartTime > DateTime.UtcNow.Subtract(StaleInProgressBuild));

                if (build == null)
                {
                    await logger("Couldn't find in progress, fetching latest");
                    build = await buildClient.GetLatestBuildAsync(
                        adoPipelineSettingsModel.Project,
                        adoPipelineSettingsModel.DefinitionId.ToString(),
                        branchName: adoPipelineSettingsModel.FullBranchName());
                }

                return GetStatusImage(build);
            }
            catch (Exception ex)
            {
                await logger($"Failed to fetch. Error: {ex}");
                return "images/actionDefaultImage@2x.png";
            }
        }

        /// <summary>
        /// Gets the build status of the latest run of a stage in a release pipeline.
        /// </summary>
        /// <param name="adoPipelineSettingsModel">Settings with the information about the pipeline.</param>
        /// <returns>The latest release information</returns>
        public async Task<string> GetReleaseStageInformation(AdoPipelineSettingsModel adoPipelineSettingsModel)
        {
            var connection = GetConnection(adoPipelineSettingsModel);
            var releaseClient = connection.GetClient<ReleaseHttpClient>();

            // Prioritize active releases over queued/completed
            var releases = await releaseClient.GetDeploymentsAsync(
                adoPipelineSettingsModel.Project,
                definitionId: adoPipelineSettingsModel.DefinitionId,
                definitionEnvironmentId: adoPipelineSettingsModel.EnvironmentId,
                queryOrder: ReleaseQueryOrder.Descending,
                deploymentStatus: DeploymentStatus.InProgress,
                latestAttemptsOnly: true,
                top: 10);

            var deployment = releases?.FirstOrDefault(x => x.QueuedOn > DateTime.UtcNow.Subtract(StaleInProgressBuild));

            // Fetch latest release if none active.
            if (deployment == null)
            {
                releases = await releaseClient.GetDeploymentsAsync(
                    adoPipelineSettingsModel.Project,
                    definitionId: adoPipelineSettingsModel.DefinitionId,
                    definitionEnvironmentId: adoPipelineSettingsModel.EnvironmentId,
                    queryOrder: ReleaseQueryOrder.Descending,
                    latestAttemptsOnly: true,
                    top: 10);

                deployment = releases?.FirstOrDefault(r => r.DeploymentStatus != DeploymentStatus.NotDeployed);
            }

            return GetStatusImage(deployment);
        }

        private string GetStatusImage(Deployment deployment)
        {
            switch (deployment?.DeploymentStatus)
            {
                case DeploymentStatus.Succeeded:
                    return "images/success@2x.png";
                case DeploymentStatus.Failed:
                    return "images/fail@2x.png";
                case DeploymentStatus.InProgress:
                    return "images/inProgress@2x.png";
                case DeploymentStatus.PartiallySucceeded:
                    return "images/partial@2x.png";
                default:
                    return "images/actionDefaultImage@2x.png";
            }
        }

        private string GetStatusImage(Build build)
        {
            return build?.Status switch
            {
                null => "images/actionDefaultImage@2x.png",
                BuildStatus.Completed when build.Result == BuildResult.Succeeded => "images/success@2x.png",
                BuildStatus.Completed when build.Result == BuildResult.Failed => "images/fail@2x.png",
                BuildStatus.Completed when build.Result == BuildResult.Canceled => "images/cancel@2x.png",
                BuildStatus.Completed when build.Result == BuildResult.PartiallySucceeded => "images/partial@2x.png",
                BuildStatus.Cancelling => "images/cancel@2x.png",
                BuildStatus.InProgress => "images/inProgress@2x.png",
                _ => "images/actionDefaultImage@2x.png",
            };
        }

        private VssConnection GetConnection(AdoSettingsModel adoPipelineSettingsModel)
        {
            var credentials = new VssBasicCredential(string.Empty, adoPipelineSettingsModel.PAT);
            return new VssConnection(new Uri(string.Format(AdoOrganizationUrl, adoPipelineSettingsModel.Organization)), credentials);
        }
    }
}
