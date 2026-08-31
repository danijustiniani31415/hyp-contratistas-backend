namespace Abril_Backend.Shared.Services.SharePoint.Options
{
    public class OneDriveOptions
    {
        public AdjudicacionesFeatureOptions AdjudicacionesFeature { get; set; } = new();

        public class AdjudicacionesFeatureOptions
        {
            public InstructivosOptions Instructivos { get; set; } = new();
        }

        public class InstructivosOptions
        {
            public string DriveId { get; set; } = string.Empty;
            public string FolderPath { get; set; } = string.Empty;
        }
    }
}
