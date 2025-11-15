using CloudStorages.Server.Dtos.Requests;
using CloudStorages.Server.Dtos.Responses;
using Microsoft.AspNetCore.Mvc;

namespace CloudStorages.Server.Services
{
    public interface IStorageService
    {
        Task<GetUploadUrlResponse> GetUploadUrl(GetUploadUrlRequest request);
        string GetDownloadUrl(GetDownloadUrlRequest request);

        Task<List<UploadFileResponse>> UploadFilesAsync(UploadFilesRequest request);
        Task<FileStreamResult> DownloadFileAsync(string key);
        Task<GetAllFilesResponse> GetAllFilesAsync(GetAllFilesRequest request);
        Task DeleteFileAsync(string key);

        Task<bool> CreateContainerAsync(string containerName);
        Task<IEnumerable<ContainerResponse>> GetAllContainersAsync();
        Task<bool> DeleteContainerAsync(string containerName);
        Task<bool> CheckContainerExistsAsync(string containerName);

    }
}
