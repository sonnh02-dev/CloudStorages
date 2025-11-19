using System.ComponentModel.DataAnnotations;

namespace CloudStorages.Server.Dtos.Requests
{
    public sealed record GetAllFilesRequest
    {

        [Required]
        public string Prefix { get; init; }= string.Empty;

        public int? MaxKeys { get; init; }

        /// Token để lấy trang kế tiếp 
        public string? ContinuationToken { get; init; }

       
    }
}
