namespace CloudStorages.Server.Dtos.Responses
{
    public sealed  record InitiatePartsUploadResponse
    {
        public string Key { get; init; } = string.Empty;
        public string UploadId { get; init; } = string.Empty;
    }

}
