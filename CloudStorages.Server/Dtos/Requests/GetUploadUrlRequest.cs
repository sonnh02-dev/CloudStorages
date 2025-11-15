namespace CloudStorages.Server.Dtos.Requests
{
    public sealed record GetUploadUrlRequest(
        string FileName,
        string ContentType,
        string? Prefix = "uploads",
        int ExpiresInMinutes = 15
    );
}
