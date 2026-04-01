using Microsoft.OpenApi.Models;
// Add this using directive

var builder = WebApplication.CreateBuilder(args);

// Read configurations
var tenantId = builder.Configuration["AzureAd:TenantId"];
var clientId = builder.Configuration["AzureAd:ClientId"];
var scopeName = builder.Configuration["AzureAd:Scopes"];
var scopeUrl = $"api://{clientId}/{scopeName}";


// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddControllers();
// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.OAuth2,
        Flows = new OpenApiOAuthFlows
        {
            AuthorizationCode = new OpenApiOAuthFlow
            {
                // Replace YOUR_TENANT_ID with your actual tenant ID
                AuthorizationUrl = new Uri($"https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/authorize"),
                TokenUrl = new Uri($"https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/token"),
                Scopes = new Dictionary<string, string>
                {
                    // Replace YOUR_API_CLIENT_ID with your actual client ID
                    { scopeUrl, "Access API as user" }
                }
            }
        }
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "oauth2"
                }
            },
            new[] { scopeUrl }
        }
    });
});


var app = builder.Build();

// Configure the HTTP request pipeline.     
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Image Upload API v1");
    
    // OAuth2 setup for Swagger UI
    c.OAuthClientId(clientId);
    c.OAuthUsePkce(); // Use Authorization Code flow with PKCE
    c.OAuthScopes(scopeUrl);
    c.OAuthAdditionalQueryStringParams(new Dictionary<string, string> 
    { 
        {"resource", $"api://{clientId}"} 
    });
    
    // Force Swagger to be at exactly the root URL
    c.RoutePrefix = string.Empty;
    
    // Explicitly configure the correct absolute callback path for Azure AD OAuth2
    c.OAuth2RedirectUrl("https://upload-image-api.azurewebsites.net/oauth2-redirect.html");
});

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();


