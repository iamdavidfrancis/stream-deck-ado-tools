using StreamDeckAzureDevOps.Models;
using StreamDeckAzureDevOps.Services.ResponseModels;
using StreamDeckAzureDevOps.Utilities;
using System;
using System.Collections.Generic;
using System.Globalization;
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
        private const string AdoApiUrl = "https://vsrm.dev.azure.com/{0}/{1}/_apis/{2}?api-version=6.1-preview.2&{3}";
        private const string AdoDeploymentsPath = "release/deployments";

        private const string AdoDeploymentsQuery = "definitionId={0}&definitionEnvironmentId={1}&latestAttemptsOnly=true&$top=10";

        private readonly JsonSerializerOptions defaultJsonSerializerOptions = new JsonSerializerOptions { AllowTrailingCommas = true, IgnoreNullValues = true, PropertyNameCaseInsensitive = true, Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) } };

        private readonly HttpClient httpClient;
        private readonly Logger logger;

        public delegate Task Logger(string message);

        public AzureDevOpsService(Logger logger)
        {
            this.logger = logger;
            this.httpClient = new HttpClient();
        }

        /// <summary>
        /// Gets the build status of the latest run of a stage in a release pipeline.
        /// </summary>
        /// <param name="adoPipelineSettingsModel">Settings with the information about the pipeline.</param>
        /// <returns>The latest release information</returns>
        public async Task<ReleaseDeploymentsResponse> GetReleaseStageInformation(AdoPipelineSettingsModel adoPipelineSettingsModel)
        {
            var qsp = string.Format(CultureInfo.InvariantCulture, AdoDeploymentsQuery, adoPipelineSettingsModel.DefinitionId, adoPipelineSettingsModel.EnvironmentId);
            var message = BuildHttpRequest(HttpMethod.Get, adoPipelineSettingsModel, AdoDeploymentsPath, qsp);

            try
            {
                var response = await this.httpClient.SendAsync(message);
                var body = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    await logger(body);
                    return null;
                }

                var parsed = JsonSerializer.Deserialize<WrappedResponse<ReleaseDeploymentsResponse>>(body, this.defaultJsonSerializerOptions);

                foreach (var item in parsed.Value)
                { 
                    if (item.DeploymentStatus != ReleaseEnvironmentStatus.NotDeployed)
                    {
                        return item;
                    }
                }

                return parsed.Value[0];
            }
            catch (Exception ex)
            {
                await logger(ex.ToString());
                return null;
            }
        }

        private static HttpRequestMessage BuildHttpRequest(HttpMethod method, AdoSettingsModel adoSettingsModel, string relativePath, string queryStringParams)
        {
            var url = BuildAdoUrl(adoSettingsModel, relativePath, queryStringParams);

            var request = new HttpRequestMessage(method, url);
            request.Headers.Authorization = HttpUtilities.GetBasicAuthHeader(adoSettingsModel.Username, adoSettingsModel.PAT);

            return request;
        }

        private static string BuildAdoUrl(AdoSettingsModel adoSettingsModel, string relativePath, string queryStringParams)
        {
            return string.Format(CultureInfo.InvariantCulture, AdoApiUrl, adoSettingsModel.Organization, adoSettingsModel.Project, relativePath, queryStringParams);
        }
    }
}
