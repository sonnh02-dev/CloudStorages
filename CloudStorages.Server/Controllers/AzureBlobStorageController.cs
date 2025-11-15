using CloudStorages.Server.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CloudStorages.Server.Controllers
{
    [ApiController]
    [Route("api/azure-blob-storage")]

    public class AzureBlobStorageController : StorageController<IAzureBlobStorageService>
    {
        public AzureBlobStorageController(IAzureBlobStorageService storageService)
            : base(storageService)
        {
        }
       
    }
}
