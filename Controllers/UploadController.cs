using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ImageUploadApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UploadController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public UploadController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpPost("image")]
        [Authorize] // Only authenticated users can access
        public async Task<IActionResult> UploadImage(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded.");

            try
            {
                // Get Key Vault details from configuration
                string keyVaultUrl = _configuration["KeyVault:Url"];
                string secretName = _configuration["KeyVault:BlobConnectionSecretName"];

                // Authenticate with Managed Identity
                var client = new SecretClient(new Uri(keyVaultUrl), new DefaultAzureCredential());

                // Retrieve Blob connection string from Key Vault
                KeyVaultSecret secret = await client.GetSecretAsync(secretName);
                string blobConnectionString = secret.Value;

                // Connect to Blob Storage
                var blobServiceClient = new BlobServiceClient(blobConnectionString);
                var containerClient = blobServiceClient.GetBlobContainerClient("images");

                // Ensure container exists
                await containerClient.CreateIfNotExistsAsync();

                // Upload file
                string blobName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                var blobClient = containerClient.GetBlobClient(blobName);

                using (var stream = file.OpenReadStream())
                {
                    await blobClient.UploadAsync(stream, overwrite: true);
                }

                return Ok(new { message = "Successfully uploaded", blobName });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}