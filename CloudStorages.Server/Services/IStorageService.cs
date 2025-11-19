using CloudStorages.Server.Dtos.Requests;
using CloudStorages.Server.Dtos.Responses;
using Microsoft.AspNetCore.Mvc;

namespace CloudStorages.Server.Services
{
    public interface IStorageService
    {
        GetUploadUrlResponse GetUploadUrl(GetUploadUrlRequest request);
        string GetDownloadUrl(GetDownloadUrlRequest request);

        Task<List<UploadFileResponse>> UploadFilesAsync(UploadFilesRequest request);
        Task<FileStreamResult> DownloadFileAsync(string fileKey);
        Task<GetAllFilesResponse> GetAllFilesAsync(GetAllFilesRequest request);
        Task DeleteFileAsync(string fileKey);

        Task<bool> CreateContainerAsync(string containerName);
        Task<IEnumerable<ContainerResponse>> GetAllContainersAsync();
        Task<bool> DeleteContainerAsync(string containerName);
        Task<bool> CheckContainerExistsAsync(string containerName);

    }
}
