namespace CloudStorages.Server.Configuration
{
    public sealed class AzureBlobSettings
    {
        public string ConnectionString { get; init; } = string.Empty;

        public string AccountName { get; init; } = string.Empty;

        public string ContainerName { get; init; } = string.Empty;
    }
}
