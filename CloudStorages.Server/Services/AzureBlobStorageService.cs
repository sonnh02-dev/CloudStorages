using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using CloudStorages.Server.Configuration;
using CloudStorages.Server.Dtos.Requests;
using CloudStorages.Server.Dtos.Responses;
using CloudStorages.Server.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Text;
using FileInfo = CloudStorages.Server.Dtos.Responses.FileInfo;

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

        public GetUploadUrlResponse GetUploadUrl(GetUploadUrlRequest request)
        {
            var blobKey = PathHelper.BuildKey(request.Prefix);

            var blobClient = _containerClient.GetBlobClient(blobKey);
            var sasBuilder = new BlobSasBuilder
            {
                BlobContainerName = _containerName,
                BlobName = blobKey,
                Resource = "b",// 'b' for blob , 'c' for container
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
                var fileKey = PathHelper.BuildKey(request.Prefix);
                var blobClient = _containerClient.GetBlobClient(fileKey); //tạo blob mới nếu chưa tồn tại.

                await using var stream = file.OpenReadStream();
                var metadata = new Dictionary<string, string> {
                    {"fileName",Uri.EscapeDataString(file.FileName)} //metadata key chỉ gồm a–z, A-Z, 0–9 và dấu gạch dưới (_)

                };
                await blobClient.UploadAsync(stream, new BlobUploadOptions { Metadata = metadata });

                results.Add(new UploadFileResponse
                {
                    Key = fileKey,
                    FileName = file.FileName
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
                ExpiresOn = DateTimeOffset.UtcNow.AddMinutes(request.ExpiresInMinutes)
            };
            sasBuilder.SetPermissions(BlobSasPermissions.Read);

            return blobClient.GenerateSasUri(sasBuilder).ToString();
        }

        public async Task<FileStreamResult> DownloadFileAsync(string fileKey)
        {
            var blobClient = _containerClient.GetBlobClient(fileKey);

            var response = await blobClient.DownloadAsync();
            var metadata = response.Value.Details.Metadata;
            var fileName = metadata.TryGetValue("fileName", out var name) ? Uri.UnescapeDataString(name) : "unknown";

            var stream = response.Value.Content;
            var contentType = response.Value.Details.ContentType ?? "application/octet-stream";

            return new FileStreamResult(stream, contentType)
            {
                FileDownloadName = fileName
            };
        }

        public async Task<GetAllFilesResponse> GetAllFilesAsync(GetAllFilesRequest request)
        {
            var files = new List<FileInfo>();

            // Lấy blob theo prefix và phân trang
            IAsyncEnumerable<Page<BlobItem>> blobPages = _containerClient
                .GetBlobsAsync(prefix: request.Prefix)
                .AsPages(request.ContinuationToken, request.MaxKeys);

            string? continuationToken = null;
            bool isTruncated = false;

            await foreach (Page<BlobItem> page in blobPages)
            {
                foreach (BlobItem blob in page.Values)
                {
                    files.Add(new FileInfo
                    {
                        Key = blob.Name,
                        FileName = blob.Metadata.TryGetValue("fileName", out var metaName)
                            ? Uri.UnescapeDataString(metaName)
                            : "Unnamed (no metadata)",

                        Size = blob.Properties.ContentLength,
                        LastModified = blob.Properties.LastModified?.DateTime
                    });
                }

                continuationToken = page.ContinuationToken;
                isTruncated = !string.IsNullOrEmpty(continuationToken);

                // Dừng nếu đủ số lượng yêu cầu
                if (request.MaxKeys.HasValue && files.Count >= request.MaxKeys.Value)
                    break;
            }

            return new GetAllFilesResponse
            {
                Files = files,
                ContinuationToken = continuationToken,
                IsTruncated = isTruncated
            };
        }


        public async Task DeleteFileAsync(string fileKey)
        {
            var blobClient = _containerClient.GetBlobClient(fileKey);

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
