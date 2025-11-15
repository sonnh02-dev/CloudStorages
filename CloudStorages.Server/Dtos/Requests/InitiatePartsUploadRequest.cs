namespace CloudStorages.Server.Dtos.Requests
{
    public sealed class InitiatePartsUploadRequest
    {
        public string FileName { get; init; } = string.Empty;
        public string ContentType { get; init; } = string.Empty;

        public string? Prefix { get; init; } = "uploads";


    }
}
