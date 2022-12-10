using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.ReleaseManagement.WebApi;
using Microsoft.VisualStudio.Services.ReleaseManagement.WebApi.Clients;
using Microsoft.VisualStudio.Services.WebApi;
using StreamDeckAzureDevOps.Models;
using StreamDeckAzureDevOps.Utilities;
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
        /// Gets the build status of the latest run of a stage in a release pipeline.
        /// </summary>
        /// <param name="adoPipelineSettingsModel">Settings with the information about the pipeline.</param>
        /// <returns>The latest release information</returns>
        public async Task<Deployment> GetReleaseStageInformation(AdoPipelineSettingsModel adoPipelineSettingsModel)
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

                deployment = releases?.FirstOrDefault();
            }

            return deployment;
        }

        private VssConnection GetConnection(AdoPipelineSettingsModel adoPipelineSettingsModel)
        {
            var credentials = new VssBasicCredential(string.Empty, adoPipelineSettingsModel.PAT);
            return new VssConnection(new Uri(string.Format(AdoOrganizationUrl, adoPipelineSettingsModel.Organization)), credentials);
        }
    }
}
