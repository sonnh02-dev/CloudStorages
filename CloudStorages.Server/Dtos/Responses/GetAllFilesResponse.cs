
namespace CloudStorages.Server.Dtos.Responses
{
    public sealed record GetAllFilesResponse
    {
        public List<FileInfo> Files { get; init; } = new();
        public string? ContinuationToken { get; init; }
        public bool? IsTruncated { get; init; } // true = còn dữ liệu
    }
    public sealed record FileInfo
    {
        public string Key { get; init; } = default!;
        public string FileName { get; init; } = default!;
        public long? Size { get; init; }
        public DateTime? LastModified { get; init; }
    }
}