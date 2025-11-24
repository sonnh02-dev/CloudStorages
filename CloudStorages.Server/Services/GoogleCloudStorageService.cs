using CloudStorages.Server.Configuration;
using CloudStorages.Server.Dtos.Requests;
using CloudStorages.Server.Dtos.Responses;
using CloudStorages.Server.Utils;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Storage.v1;
using Google.Apis.Storage.v1.Data;
using Google.Cloud.Storage.V1;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using FileInfo = CloudStorages.Server.Dtos.Responses.FileInfo;

namespace CloudStorages.Server.Services
{
    public class GoogleCloudStorageService : IGoogleCloudStorageService
    {
        private readonly StorageClient _storageClient;
        private readonly UrlSigner _urlSigner;
        private readonly string _bucketName;
        private readonly string _projectId;

        public GoogleCloudStorageService(IOptions<GoogleCloudSettings> options)
        {
            var settings = options.Value;
            _bucketName = settings.BucketName;
            _projectId = settings.ProjectId;

            var credential = GoogleCredential
              .FromJson(settings.Credential)
              .CreateScoped(StorageService.Scope.CloudPlatform);

            _storageClient = StorageClient.Create(credential);
            _urlSigner = UrlSigner.FromCredential(credential);
        }


        public GetUploadUrlResponse GetUploadUrl(GetUploadUrlRequest request)
        {
            var objectKey = PathHelper.BuildKey(request.Prefix);

            var url = _urlSigner.Sign(
                _bucketName,
                objectKey,
                TimeSpan.FromMinutes(request.ExpiresInMinutes),
                HttpMethod.Put

            );

            return new GetUploadUrlResponse(objectKey, url);
        }



        public async Task<List<UploadFileResponse>> UploadFilesAsync(UploadFilesRequest request)
        {
            var results = new List<UploadFileResponse>();

            foreach (var file in request.Files)
            {
                var objectKey = PathHelper.BuildKey(request.Prefix);

                await using var stream = file.OpenReadStream();

                var gcsObject = new Google.Apis.Storage.v1.Data.Object
                {
                    Bucket = _bucketName,
                    Name = objectKey,// mặc định ghi đè
                    ContentType = string.IsNullOrEmpty(file.ContentType) ? "application/octet-stream" : file.ContentType,
                    Metadata = new Dictionary<string, string> { { "fileName", Uri.EscapeDataString(file.FileName) } }
                };

                var options = new UploadObjectOptions
                {
                    // IfGenerationMatch = 0, // file đã tồn tại → throw 409 conflict
                    ChunkSize = 10 * 1024 * 1024 // upload file 10 MB chunk
                };

                var uploaded = await _storageClient.UploadObjectAsync(gcsObject, stream, options);

                results.Add(new UploadFileResponse
                {
                    Key = objectKey,
                    FileName = file.FileName
                });
            }

            return results;
        }




        public string GetDownloadUrl(GetDownloadUrlRequest request)
        {
            return _urlSigner.Sign(
                _bucketName,
                request.FileKey,
                TimeSpan.FromMinutes(request.ExpiresInMinutes),
                HttpMethod.Get
            );
        }


        public async Task<FileStreamResult> DownloadFileAsync(string fileKey)
        {
            var memory = new MemoryStream();

            var obj = await _storageClient.GetObjectAsync(_bucketName, fileKey);

            await _storageClient.DownloadObjectAsync(_bucketName, fileKey, memory);
            memory.Position = 0;

            var fileName = obj.Metadata != null &&
                           obj.Metadata.TryGetValue("fileName", out var metaName)
                           ? Uri.UnescapeDataString(metaName)
                           : "unknown";

            return new FileStreamResult(memory, obj.ContentType ?? "application/octet-stream")
            {
                FileDownloadName = fileName
            };
        }


        public async Task<GetAllFilesResponse> GetAllFilesAsync(GetAllFilesRequest request)
        {
            var pageSize = request.MaxKeys ?? 20;
            var page = await _storageClient.ListObjectsAsync(
                   _bucketName,
                   request.Prefix,
                   new ListObjectsOptions { PageToken = request.ContinuationToken }
               ).ReadPageAsync(request.ContinuationToken==null?++pageSize:pageSize);   


            var files = page
                .Where(o => o.Size > 0)          
                .Select(obj => new FileInfo
                {
                    Key = obj.Name,
                    FileName = obj.Metadata != null &&
                               obj.Metadata.TryGetValue("fileName", out var metaName)
                                   ? Uri.UnescapeDataString(metaName)
                                   : "Unnamed (no metadata)",
                    Size = (long?)obj.Size,
                    LastModified = obj.UpdatedDateTimeOffset?.UtcDateTime
                })
                .ToList();

            return new GetAllFilesResponse
            {
                Files = files,
                ContinuationToken = page.NextPageToken,
                IsTruncated = !string.IsNullOrEmpty(page.NextPageToken)
            };
        }



        public async Task DeleteFileAsync(string fileKey)
        {
            await _storageClient.DeleteObjectAsync(_bucketName, fileKey);
        }

        public async Task<bool> CreateBucketAsync(string bucketName)
        {
            await _storageClient.CreateBucketAsync(_projectId, new Bucket
            {
                Name = bucketName
            });

            return true;
        }

        public async Task<IEnumerable<ContainerResponse>> GetAllContainersAsync()
        {
            var list = new List<ContainerResponse>();

            foreach (var bucket in _storageClient.ListBuckets(_projectId))
            {
                list.Add(new ContainerResponse { Name = bucket.Name });
            }

            return list;
        }

        public async Task<bool> DeleteContainerAsync(string containerName)
        {
            await _storageClient.DeleteBucketAsync(containerName);
            return true;
        }

        public async Task<bool> CheckContainerExistsAsync(string containerName)
        {
            try
            {
                await _storageClient.GetBucketAsync(containerName);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> CreateContainerAsync(string containerName)
        {
            await _storageClient.CreateBucketAsync(_projectId, new Bucket
            {
                Name = containerName
            });

            return true;
        }
    }
}
