# CloudStorages.Server

- A modular and extensible .NET 7 Web API that provides unified file-storage operations across multiple providers, including Amazon S3 and Azure Blob Storage.
  The project implements common storage endpoints via a generic controller and provider-specific service implementations.

## Table of Contents

- [Project structure](#-project-structure)
- [Features](#-features)
- [How to run](#-how-to-run)
  - [Clone repo](#1-clone-repo)
  - [Install prerequisites](#2-install-prerequisites)
  - [Restore dependencies](#3-restore-dependencies)
  - [Configure storage providers](#4-configure-storage-providers)
  - [Retrieve and store secrets](#5retrieve-and-store-secrets-in-storage-providers)
  - [Application configuration](#6-application-configuration)
  - [Run the API](#7-run-the-api)
- [License](#-license)

## 📁 Project structure

```
CloudStorages.Server/
│
├── Configuration/
│   ├── AwsS3Settings.cs
│   └── AzureBlobSettings.cs
│
├── Controllers/
│   ├── AwsS3StorageController.cs
│   ├── AzureBlobStorageController.cs
│   └── StorageController.cs               # Generic base controller
│
├── Dtos/
│   ├── Requests/
│   └── Responses/
│
├── Filters/
│   └── DecodeRouteKeyAttribute.cs         # Automatically decodes the file key
│
├── Middlewares/
│   └── ExceptionHandlingMiddleware.cs     # Global exception handler
│
├── Services/
│   ├── AwsS3StorageService.cs
│   ├── AzureBlobStorageService.cs
│   ├── IAwsS3StorageService.cs
│   ├── IAzureBlobStorageService.cs
│   └── IStorageService.cs                 # Common interface
│
├── Utils/
│
├── appsettings.json
└── Program.cs

```

## ✨ Features

### ✔ Common Storage Operations

- All providers implement:

  - Generate upload URL

  - Generate download URL

  - Upload file(s)

  - Download file

  - List all files

  - Delete file

  - Create container

  - List containers

  - Check container existence

### ✔ AWS S3 extra features

- Multipart upload

  - Pre-signed URLs for each upload part

  - Complete multipart upload

### ✔ Unified controller design

The base class:

```
StorageController<TStorageService>
```

automatically provides all core REST endpoints, so each provider only needs to register its service.

## 🚀 How to run

### 1. Clone repo

```
git clone https://github.com/sonnh02-dev/CloudStorages.git
cd CloudStorages.Server
```

### 2. Install prerequisites

- .NET SDK 7+

- Azure CLI (for Key Vault integration)

- Cloud resources:

  - AWS S3 Bucket
  - Azure Blob Storage account + container

### 3. Restore & build

- `dotnet restore`
- `dotnet build`

### 4. Configure storage providers

- For AWS S3 :

  - Create bucket  
    ![AWS S3 Bucket](docs/images/aws-s3-bucket.png)

  - Configure CORS  
    ![AWS S3 CORS](docs/images/aws-s3-cors.png)

- For Azure Blob :

  - Create storage account, container  
    ![Azure Blob Container](docs/images/azure-blob-container.png)

  - Configure CORS  
    ![Azure Blob CORS](docs/images/azure-blob-cors.png)

---

### 5. Retrieve and store secrets in storage providers

- Retrieve secrets

  - **AWS S3**: retrieve the access key and secret key from AWS IAM. If the secret key has been lost, generate a new one.  
    ![AWS IAM Keys](docs/images/aws-iam-keys.png)

  - **Azure Blob**: retrieve the connection string from Azure Blob Access Keys.  
    ![Azure Blob Keys](docs/images/azure-blob-keys.png)

- Store Secrets in Azure Key Vault

  - Set secret values in Azure Key Vault  
    ![Azure Key Vault Secrets](docs/images/azure-keyvault-secrets.png)

  - Secrets must follow this naming pattern for automatic configuration mapping:
    ```
    AwsS3--AccessKey
    AwsS3--SecretKey
    AzureBlob--ConnectionString
    ```


### 6. Application configuration

- Combine secrets with appsettings.json:

  ```
  {
    "KeyVaultName": "cloud-storages-kv",

    "AwsS3": {
      "BucketName": "cloud-storages-bucket",
      "Region": "ap-southeast-1"
     },

    "AzureBlob": {
      "AccountName": "cloudstoragessa",
      "ContainerName": "cloud-storages-container"
    }
  }
  ```

- Secrets are automatically loaded using:

  ```
  builder.Configuration.AddAzureKeyVault(keyVaultUri, new DefaultAzureCredential());
  ```

### 7. Run the API

- Swagger UI will be available at:
  `https://localhost:<port>/swagger`
- Dependency Injection Setup
  ```
  builder.Services.AddScoped<IAwsS3StorageService, AwsS3StorageService>();
  builder.Services.AddScoped<IAzureBlobStorageService, AzureBlobStorageService>();
  ...
  ```
  Each controller consumes the corresponding service automatically.

## 📄 License

This project is open-source for educational and personal use.
