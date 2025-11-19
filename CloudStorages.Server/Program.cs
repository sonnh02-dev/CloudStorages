using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Azure.Identity;
using Azure.Storage.Blobs;
using CloudStorages.Server.Configuration;
using CloudStorages.Server.Filters;
using CloudStorages.Server.Middlewares;
using CloudStorages.Server.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers(options =>
{
    // Global action filter để tự decode route "key"
    options.Filters.Add<DecodeRouteKeyAttribute>();
})
.AddJsonOptions(options =>
{
    // Chuyển property JSON sang camelCase
    options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors();

// Load secrets from Azure Key Vault into Configuration
var keyVaultName = builder.Configuration["KeyVaultName"];
var keyVaultUri = new Uri($"https://{keyVaultName}.vault.azure.net/");
builder.Configuration.AddAzureKeyVault(keyVaultUri, new DefaultAzureCredential());

// Auto-merge config from appsettings.json and key vault
// Key vault secrets name must follow "AwsS3--AccessKey" format to map into AwsS3Settings

builder.Services.Configure<AwsS3Settings>(

    builder.Configuration.GetSection("AwsS3"));
builder.Services.Configure<AzureBlobSettings>
   (builder.Configuration.GetSection("AzureBlob"));

// Register Amazon S3 client
builder.Services.AddSingleton<IAmazonS3>(sp =>
{
    var awsS3Settings = sp.GetRequiredService<IOptions<AwsS3Settings>>().Value;
    return new AmazonS3Client(
        new BasicAWSCredentials(awsS3Settings.AccessKey, awsS3Settings.SecretKey),
        RegionEndpoint.GetBySystemName(awsS3Settings.Region)
    );
});
//Register BlobServiceClient 
builder.Services.AddSingleton(sp =>
{
    var azureBlobSettings = sp.GetRequiredService<IOptions<AzureBlobSettings>>().Value;
    return new BlobServiceClient(azureBlobSettings.ConnectionString);
});



builder.Services.AddScoped<IAwsS3StorageService, AwsS3StorageService>();
builder.Services.AddScoped<IAzureBlobStorageService,AzureBlobStorageService>();


var app = builder.Build();

app.UseRouting();

app.UseCors(policy =>
    policy.AllowAnyOrigin()
          .AllowAnyMethod()
          .AllowAnyHeader());

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.MapControllers();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.Run();