namespace StreamDeckAzureDevOps.Models
{
    public class AdoPipelineSettingsModel : AdoSettingsModel
    {
        public int DefinitionId { get; set; }
        public int EnvironmentId { get; set; }
    }
}
