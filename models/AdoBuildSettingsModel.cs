namespace StreamDeckAzureDevOps.Models
{
    public class AdoBuildSettingsModel : AdoSettingsModel
    {
        public int DefinitionId { get; set; }
        public string BranchName { get; set; }

        public string FullBranchName() => !string.IsNullOrEmpty(BranchName) ? $"refs/heads/{BranchName}" : null;
    }
}
