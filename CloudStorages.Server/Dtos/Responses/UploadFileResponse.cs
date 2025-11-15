namespace CloudStorages.Server.Dtos.Responses
{
    public sealed record UploadFileResponse
    {
        public string Key { get; init; }=default!;
        public string FileName { get; init; } = default!;
    }
}
