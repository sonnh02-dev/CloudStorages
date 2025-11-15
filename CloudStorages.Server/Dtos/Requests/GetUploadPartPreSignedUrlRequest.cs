namespace CloudStorages.Server.Dtos.Requests
{
    public sealed record GetUploadPartPreSignedUrlRequest(
     string UploadId,
     int PartNumber,
     string? FileKey = null
 );

}
