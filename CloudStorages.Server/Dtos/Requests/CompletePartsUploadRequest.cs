using System.ComponentModel.DataAnnotations;

namespace CloudStorages.Server.Dtos.Requests
{
    public sealed record CompletePartsUploadRequest(
        [Required] string UploadId,
        [Required] List<PartETagInfo> Parts,
        string? FileKey = null
    );

    public sealed record PartETagInfo(
        [Required] int PartNumber,
        [Required] string ETag
    );
}
