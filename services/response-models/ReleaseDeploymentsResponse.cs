using System;
using System.Collections.Generic;
using System.Text;

namespace StreamDeckAzureDevOps.Services.ResponseModels
{
    public class ReleaseDeploymentsResponse
    {
        public int Id { get; set; }
        public ReleaseEnvironmentStatus DeploymentStatus { get; set; }
        public ReleaseEnvironment ReleaseEnvironment { get; set; }
    }
}
