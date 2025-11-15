using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Util;
using CloudStorages.Server.Configuration;
using CloudStorages.Server.Dtos.Requests;
using CloudStorages.Server.Dtos.Responses;
using CloudStorages.Server.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using System.Net;
using System.Text;

namespace CloudStorages.Server.Services
{
    internal class AwsS3StorageService : IAwsS3StorageService
    {
        private readonly IAmazonS3 _s3Client;
        private readonly string _bucketName;


        public AwsS3StorageService(
            IAmazonS3 s3Client,
            IOptions<AwsS3Settings> options
            )
        {
            _s3Client = s3Client;
            _bucketName = options.Value.BucketName;
        }
        //Upload trực tiếp lên S3 (dùng Pre-signed URL)
        public async Task<GetUploadUrlResponse> GetUploadUrl(GetUploadUrlRequest request)
        {
            var objectKey = PathHelper.BuildKey(request.Prefix);
            var preSignedRequest = new GetPreSignedUrlRequest
            {
                BucketName = _bucketName,
                Key = objectKey,
                Verb = HttpVerb.PUT,
                Expires = DateTime.UtcNow.AddMinutes(request.ExpiresInMinutes),
                ContentType = request.ContentType,
                Metadata = { ["file-name"] = Uri.EscapeDataString(request.FileName) }

            };

            return new GetUploadUrlResponse(objectKey, _s3Client.GetPreSignedURL(preSignedRequest));
        }

        public string GetDownloadUrl(GetDownloadUrlRequest request)
        {

            var preSignedRequest = new GetPreSignedUrlRequest
            {
                BucketName = _bucketName,
                Key = request.FileKey,
                Verb = HttpVerb.GET,
                Expires = DateTime.UtcNow.AddMinutes(request.ExpiresInMinutes)
            };
            return _s3Client.GetPreSignedURL(preSignedRequest);

        }

        //Upload qua server (dùng UploadFilesAsync())
        public async Task<List<UploadFileResponse>> UploadFilesAsync(UploadFilesRequest request)
        {
            var results = new List<UploadFileResponse>();


            foreach (var file in request.Files)
            {
                if (file.Length == 0) continue;

                var objectKey = PathHelper.BuildKey(request.Prefix);

                using var stream = file.OpenReadStream();
                var putRequest = new PutObjectRequest
                {
                    BucketName = _bucketName,
                    Key = objectKey,
                    InputStream = stream,
                    ContentType = file.ContentType,
                    Metadata = { ["file-name"] = Uri.EscapeDataString(file.FileName) }

                };


                await _s3Client.PutObjectAsync(putRequest);

                results.Add(new UploadFileResponse
                {
                    Key = objectKey,
                    FileName = file.FileName,
                });
            }

            return results;
        }


        public async Task<FileStreamResult> DownloadFileAsync(string key)
        {


            var response = await _s3Client.GetObjectAsync(new GetObjectRequest
            {
                BucketName = _bucketName,
                Key = key
            });

            var fileName = Uri.UnescapeDataString(response.Metadata["file-name"]);
            return new FileStreamResult(response.ResponseStream, response.Headers.ContentType)
            {
                FileDownloadName = fileName,
                EnableRangeProcessing = true
            };
        }

        public async Task<GetAllFilesResponse> GetAllFilesAsync(GetAllFilesRequest request)
        {
            var listRequest = new ListObjectsV2Request
            {
                BucketName = _bucketName,
                Prefix = $"{PathHelper.NormalizePrefix(request.Prefix)}/",
                ContinuationToken = request.ContinuationToken,
                MaxKeys = request.MaxKeys ?? 10

            };

            var files = new List<S3ObjectResponse>();
            var listResponse = await _s3Client.ListObjectsV2Async(listRequest);

            foreach (var obj in listResponse.S3Objects.Where(o => !o.Key.EndsWith("/")))
            {
                string? fileName = null;

                try
                {
                    // Lấy metadata
                    var metadataResponse = await _s3Client.GetObjectMetadataAsync(new GetObjectMetadataRequest
                    {
                        BucketName = _bucketName,
                        Key = obj.Key
                    });

                    // Metadata key luôn lowercase trong AWS S3
                    if (metadataResponse.Metadata.Keys.Contains("x-amz-meta-file-name"))
                    {
                        fileName = Uri.UnescapeDataString(metadataResponse.Metadata["x-amz-meta-file-name"]);
                    }
                }
                catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
                {
                    // Có thể file bị xóa trong lúc đọc metadata
                    continue;
                }

                files.Add(new S3ObjectResponse
                {
                    Key = obj.Key,
                    FileName = fileName ?? "Unnamed (no metadata)",
                    Size = obj.Size,
                    LastModified = obj.LastModified
                });
            }

            return new GetAllFilesResponse

            {
                Files = files,
                ContinuationToken = listResponse.NextContinuationToken,
                IsTruncated = listResponse.IsTruncated
            };
        }

        public async Task DeleteFileAsync(string key)
        {


            await _s3Client.DeleteObjectAsync(new DeleteObjectRequest
            {
                BucketName = _bucketName,
                Key = key,
            });
        }
        //=================================Bucket management===============================

        public async Task<bool> CreateContainerAsync(string containerName)
        {
            bool bucketExists = await CheckContainerExistsAsync(containerName);
            if (bucketExists) return false;

            await _s3Client.PutBucketAsync(new PutBucketRequest
            {
                BucketName = containerName,
                UseClientRegion = true
            });

            return true;
        }

        public async Task<IEnumerable<ContainerResponse>> GetAllContainersAsync()
        {
            var response = await _s3Client.ListBucketsAsync();
            return response.Buckets.Select(b => new ContainerResponse
            {
                Name = b.BucketName,
                CreatedOn = b.CreationDate,
            });
        }

        public async Task<bool> DeleteContainerAsync(string containerName)
        {
            bool bucketExists = await CheckContainerExistsAsync(containerName);
            if (!bucketExists) return false;

            await _s3Client.DeleteBucketAsync(new DeleteBucketRequest
            {
                BucketName = containerName
            });

            return true;
        }

        public async Task<bool> CheckContainerExistsAsync(string containerName)
        {
            return await AmazonS3Util.DoesS3BucketExistV2Async(_s3Client, containerName);
        }

        //==================Split files into many small parts, upload in parallel.======================
        public async Task<InitiatePartsUploadResponse> InitiatePartsUploadAsync(InitiatePartsUploadRequest request)
        {
            var objectKey = PathHelper.BuildKey(request.Prefix);

            var initiateRequest = new InitiateMultipartUploadRequest
            {
                BucketName = _bucketName,
                Key = objectKey,
                ContentType = request.ContentType,
                Metadata = { ["file-name"] = Uri.EscapeDataString(request.FileName) }

            };

            var response = await _s3Client.InitiateMultipartUploadAsync(initiateRequest);

            return new InitiatePartsUploadResponse
            {
                Key = objectKey,
                UploadId = response.UploadId
            };
        }


        public string GetUploadPartPreSignedUrl(GetUploadPartPreSignedUrlRequest request)
        {
            var getRequest = new GetPreSignedUrlRequest
            {
                BucketName = _bucketName,
                Key = request.FileKey,
                Verb = HttpVerb.PUT,
                Expires = DateTime.UtcNow.AddMinutes(15),
                UploadId = request.UploadId,
                PartNumber = request.PartNumber
            };

            return _s3Client.GetPreSignedURL(getRequest);
        }

        public async Task<string> CompletePartsUploadAsync(CompletePartsUploadRequest request)
        {

            var completeRequest = new CompleteMultipartUploadRequest
            {
                BucketName = _bucketName,
                Key = request.FileKey,
                UploadId = request.UploadId,
                PartETags = request.Parts.Select(p => new PartETag(p.PartNumber, p.ETag)).ToList()
            };

            var response = await _s3Client.CompleteMultipartUploadAsync(completeRequest);
            return response.Location;
        }


    }
}
