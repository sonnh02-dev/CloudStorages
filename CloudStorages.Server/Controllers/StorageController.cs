using CloudStorages.Server.Dtos.Requests;
using CloudStorages.Server.Services;
using Microsoft.AspNetCore.Mvc;

namespace CloudStorages.Server.Controllers
{
    [ApiController]
    [Route("api/{controller}")]
    public abstract class StorageController<TStorageService> : ControllerBase
        where TStorageService : IStorageService
    {
        protected readonly TStorageService _storageService;

        protected StorageController(TStorageService storageService)
        {
            _storageService = storageService;
        }



        // ===========================================================
        //                     FILE OPERATIONS
        // ===========================================================

        [HttpPost("upload-url")]
        public async Task<IActionResult> GetUploadUrl([FromBody] GetUploadUrlRequest request)
            => Ok(await _storageService.GetUploadUrl(request));

        [HttpGet("{fileKey}/download-url")]
        public IActionResult GetDownloadUrl(string fileKey,[FromQuery] GetDownloadUrlRequest request)
            => Ok(new { downloadUrl = _storageService.GetDownloadUrl(request with { FileKey = fileKey }) });

        [HttpPost("upload")]
        public async Task<IActionResult> UploadFiles([FromForm] UploadFilesRequest request)
            => Ok(await _storageService.UploadFilesAsync(request));

        [HttpGet("{fileKey}/download")]
        public async Task<IActionResult> DownloadFileAsync(string fileKey)
            => await _storageService.DownloadFileAsync(fileKey);

        [HttpGet("files")]
        public async Task<IActionResult> GetAllFiles([FromQuery] GetAllFilesRequest request)
            => Ok(await _storageService.GetAllFilesAsync(request));

        [HttpDelete("{fileKey}")]
        public async Task<IActionResult> DeleteFile(string fileKey)
        {
            await _storageService.DeleteFileAsync(fileKey);
            return Ok(new { message = $"File '{fileKey}' deleted successfully" });
        }

        // ===============================================================
        //                   CONTAINER OPERATIONS
        // ===============================================================

        [HttpPost("{containerName}")]
        public async Task<IActionResult> CreateContainer(string containerName)
        {
            bool created = await _storageService.CreateContainerAsync(containerName);
            if (!created)
                return BadRequest(new { message = $"Container '{containerName}' already exists." });

            return Created($"/api/{{controller}}/{containerName}",
                new { message = $"Container '{containerName}' created successfully." });
        }

        [HttpGet("containers")]
        public async Task<IActionResult> GetAllContainers()
            => Ok(await _storageService.GetAllContainersAsync());

        [HttpDelete("{containerName}")]
        public async Task<IActionResult> DeleteContainer(string containerName)
        {
            bool deleted = await _storageService.DeleteContainerAsync(containerName);
            if (!deleted)
                return NotFound(new { message = $"Container '{containerName}' not found." });

            return Ok(new { message = $"Container '{containerName}' deleted successfully." });
        }

        [HttpGet("{containerName}/exists")]
        public async Task<IActionResult> CheckContainerExists(string containerName)
        {
            bool exists = await _storageService.CheckContainerExistsAsync(containerName);
            return Ok(new { containerName, exists });
        }
    }
}
