using CloudStorages.Server.Dtos.Requests;
using CloudStorages.Server.Services;
using Microsoft.AspNetCore.Mvc;

namespace CloudStorages.Server.Controllers
{
    [ApiController]
    [Route("api/aws-s3-storage")]
    public class AwsS3StorageController : StorageController<IAwsS3StorageService>
    {

        public AwsS3StorageController(IAwsS3StorageService storageService)
            : base(storageService)
        {
        }
      

        [HttpPost("initiate-parts-upload")]
        public async Task<IActionResult> InitiatePartsUpload([FromBody] InitiatePartsUploadRequest request)
        {
            var result = await _storageService.InitiatePartsUploadAsync(request);
            return Ok(result);
        }



        [HttpPost("{fileKey}/upload-part-presigned-url")]
        //[DecodeRouteKey]
        public IActionResult GetUploadPartPreSignedUrl(string fileKey, [FromBody] GetUploadPartPreSignedUrlRequest request)

            => Ok(new { PreSignedUrl = _storageService.GetUploadPartPreSignedUrl(request with { FileKey = fileKey }) });


        [HttpPost("{fileKey}/complete-parts-upload")]
        public async Task<IActionResult> CompletePartsUpload(string fileKey, [FromBody] CompletePartsUploadRequest request)
            => Ok(new { fileKey, Location = await _storageService.CompletePartsUploadAsync(request with { FileKey = fileKey }) });
        
        

        
    }
}
