namespace CloudStorages.Server.Dtos.Requests
{
    public sealed record GetDownloadUrlRequest(
        string FileKey,
        int ExpiresInMinutes = 15
    );
}
