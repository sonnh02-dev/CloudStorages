using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using CloudStorages.Server.Configuration;
using CloudStorages.Server.Dtos.Requests;
using CloudStorages.Server.Dtos.Responses;
using CloudStorages.Server.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace CloudStorages.Server.Services
{
    public class AzureBlobStorageService : IAzureBlobStorageService
    {
        private readonly BlobContainerClient _containerClient;
        private readonly BlobServiceClient _blobServiceClient;
        private readonly string _containerName;

        public AzureBlobStorageService(BlobServiceClient blobServiceClient, IOptions<AzureBlobSettings> options)
        {
            _blobServiceClient = blobServiceClient;
            _containerName = options.Value.ContainerName;
            _containerClient = blobServiceClient.GetBlobContainerClient(_containerName);
        }

        public async Task<GetUploadUrlResponse> GetUploadUrl(GetUploadUrlRequest request)
        {
            var blobKey = PathHelper.BuildKey(request.Prefix);

            var blobClient = _containerClient.GetBlobClient(blobKey);
            var metadata = new Dictionary<string, string>
             {
                 { "FileName", request.FileName }
             };
            await blobClient.SetMetadataAsync(metadata);

            var sasBuilder = new BlobSasBuilder
            {
                BlobContainerName = _containerName,
                BlobName = blobKey,
                Resource = "b",
                ExpiresOn = DateTimeOffset.UtcNow.AddMinutes(request.ExpiresInMinutes),
            };

            sasBuilder.SetPermissions(BlobSasPermissions.Write | BlobSasPermissions.Create);

            var sasUri = blobClient.GenerateSasUri(sasBuilder);

            return new GetUploadUrlResponse(blobKey, sasUri.ToString());

        }

        public async Task<List<UploadFileResponse>> UploadFilesAsync(UploadFilesRequest request)
        {

            var results = new List<UploadFileResponse>();

            foreach (var file in request.Files)
            {
                var blobClient = _containerClient.GetBlobClient(file.FileName);
                await using var stream = file.OpenReadStream();
                await blobClient.UploadAsync(stream, overwrite: true);

                results.Add(new UploadFileResponse
                {
                    FileName = file.FileName,
                    //Url = blobClient.Uri.ToString()
                });
            }

            return results;
        }

        public string GetDownloadUrl(GetDownloadUrlRequest request)
        {
            var blobClient = _containerClient

                .GetBlobClient(request.FileKey);

            var sasBuilder = new BlobSasBuilder
            {
                BlobContainerName = _containerName,
                BlobName = request.FileKey,
                Resource = "b",
                ExpiresOn = DateTimeOffset.UtcNow.AddMinutes(10)
            };
            sasBuilder.SetPermissions(BlobSasPermissions.Read);

            return blobClient.GenerateSasUri(sasBuilder).ToString();
        }

        public async Task<FileStreamResult> DownloadFileAsync(string key)
        {
            var blobClient = _containerClient.GetBlobClient(key);

            var response = await blobClient.DownloadAsync();
            var stream = response.Value.Content;
            var contentType = response.Value.Details.ContentType ?? "application/octet-stream";

            return new FileStreamResult(stream, contentType)
            {
                FileDownloadName = key
            };
        }
        public async Task<GetAllFilesResponse> GetAllFilesAsync(GetAllFilesRequest request)
        {
            var result = new List<string>();

            // Lấy danh sách blob theo prefix và phân trang
            var blobsPages = _containerClient
                .GetBlobsAsync(prefix: request.Prefix)
                .AsPages(request.ContinuationToken, request.MaxKeys);

            string? nextContinuationToken = null;

            await foreach (var page in blobsPages)
            {
                foreach (var blobItem in page.Values)
                {
                    result.Add(blobItem.Name);
                }

                nextContinuationToken = page.ContinuationToken;

                if (request.MaxKeys.HasValue && result.Count >= request.MaxKeys.Value)
                    break;
            }

            return new GetAllFilesResponse
            {
                //  FileNames = result,
                ContinuationToken = nextContinuationToken
            };
        }

        public async Task DeleteFileAsync(string key)
        {
            var blobClient = _containerClient.GetBlobClient(key);

            await blobClient.DeleteIfExistsAsync();
        }
        //=================================Container  management===============================

        public async Task<bool> CreateContainerAsync(string containerName)
        {
            var response = await _blobServiceClient.CreateBlobContainerAsync(containerName);
            return response != null;
        }

        public async Task<IEnumerable<ContainerResponse>> GetAllContainersAsync()
        {
            var result = new List<ContainerResponse>();
            await foreach (var container in _blobServiceClient.GetBlobContainersAsync())
            {
                result.Add(new ContainerResponse { Name = container.Name });
            }
            return result;
        }

        public async Task<bool> DeleteContainerAsync(string containerName)
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            var response = await containerClient.DeleteIfExistsAsync();
            return response.Value;
        }

        public async Task<bool> CheckContainerExistsAsync(string containerName)
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            var exists = await containerClient.ExistsAsync();
            return exists.Value;
        }
    }
}
