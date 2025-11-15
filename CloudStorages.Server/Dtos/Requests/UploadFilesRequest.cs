using Microsoft.AspNetCore.Mvc;

namespace CloudStorages.Server.Dtos.Requests
{
    public sealed record UploadFilesRequest(
     List<IFormFile> Files,
     string? Prefix = "uploads"
 );


}
