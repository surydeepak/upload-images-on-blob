using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace ImageUploadApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UploadController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private const string _containerName = "images";
        private const string _connectionString = "Connectionstring-blobStorage";

        public UploadController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpPost("image")]
        //[Authorize] // Only authenticated users can access
        public async Task<IActionResult> UploadImage(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded.");

            try
            {
                // Get Key Vault details from configuration
                string? keyVaultUrl = _configuration["KeyVault:Url"];
                string? userManagedIdentityClientId = _configuration["KeyVault:ClientId"];

                if (string.IsNullOrEmpty(keyVaultUrl))
                {
                    throw new ArgumentNullException(nameof(keyVaultUrl), "KeyVaultUri is missing in configuration.");
                }

                // Set up credentials using the Managed Identity Client ID if provided
                var credentialOptions = new DefaultAzureCredentialOptions();
                if (!string.IsNullOrEmpty(userManagedIdentityClientId))
                {
                    credentialOptions.ManagedIdentityClientId = userManagedIdentityClientId;
                }
                var credentials = new DefaultAzureCredential(credentialOptions);

                // Connect to Azure Key Vault and retrieve the connection string secret
                var secretClient = new SecretClient(new Uri(keyVaultUrl), credentials);
                KeyVaultSecret secret = (await secretClient.GetSecretAsync(_connectionString)).Value;
                string blobConnectionString = secret.Value;

                // Connect to Blob Storage
                var blobServiceClient = new BlobServiceClient(blobConnectionString);
                var containerClient = blobServiceClient.GetBlobContainerClient(_containerName);

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