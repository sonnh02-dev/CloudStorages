namespace CloudStorages.Server.Dtos.Requests
{
    public sealed record GetUploadUrlRequest(
        string? FileName=null,
        string? ContentType = null,
        string Prefix = "uploads",
        int ExpiresInMinutes = 15
    );
}
