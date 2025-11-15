using CloudStorages.Server.Dtos.Requests;
using CloudStorages.Server.Dtos.Responses;

namespace CloudStorages.Server.Services
{
    public interface IAwsS3StorageService : IStorageService
    {
    
        Task<InitiatePartsUploadResponse> InitiatePartsUploadAsync(InitiatePartsUploadRequest request);
        string GetUploadPartPreSignedUrl(GetUploadPartPreSignedUrlRequest request);
        Task<string> CompletePartsUploadAsync(CompletePartsUploadRequest request);

      
    }
}
