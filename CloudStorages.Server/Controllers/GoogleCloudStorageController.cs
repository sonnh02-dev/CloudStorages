using CloudStorages.Server.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CloudStorages.Server.Controllers
{
    [Route("api/google-cloud-storage")]
    [ApiController]
    public class GoogleCloudStorageController : StorageController<IGoogleCloudStorageService>
    {

        public GoogleCloudStorageController(IGoogleCloudStorageService storageService)
            : base(storageService)
        {
        }
    }
}
