namespace CloudStorages.Server.Dtos.Responses
{
    public sealed record GetUploadUrlResponse(
        string Key,
        string UploadUrl
    );
}
