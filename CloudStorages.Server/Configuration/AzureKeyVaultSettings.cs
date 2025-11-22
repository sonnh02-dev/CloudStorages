namespace CloudStorages.Server.Configuration
{
    public class AzureKeyVaultSettings
    {
        public string ClientId { get; set; } = string.Empty;
        public string TenantId { get; set; } = string.Empty;
        public string VaultName { get; set; } = string.Empty;

        public Uri GetVaultUri()
        {
            return new Uri($"https://{VaultName}.vault.azure.net/");
        }
    }
}
