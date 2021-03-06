# Azure Container Instance Sample with TLS Support

This project includes an example of an ASP.NET based REST API
deployed as an [Azure Container Instance (ACI)](https://docs.microsoft.com/en-us/azure/container-instances/)
with TLS support, with the TLS certificates hosted in an
[Azure File Share](https://docs.microsoft.com/en-us/azure/storage/files/storage-how-to-create-file-share?tabs=azure-portal).

**Table of Contents:**

- [Introduction](#introduction)
- [Instructions](#instructions)
  - [1. Generate a Self-Signed TLS Certificate](#1-generate-a-self-signed-tls-certificate)
  - [2. Create a Blob File Share and Upload Certificate Data](#2-create-a-blob-file-share-and-upload-certificate-data)
  - [3. Build the Image and Publish to ACR](#3-build-the-image-and-publish-to-acr)
    - [3.1 Build the Image](#31-build-the-image)
    - [3.2 Push Image to ACR](#32-push-image-to-acr)
  - [4. Create an ACR Service Principal](#4-create-an-acr-service-principal)
  - [5. Deploy the Container](#5-deploy-the-container)
  - [6. Verify the Deployment](#6-verify-the-deployment)

## Introduction

[Azure Container Isntances](https://azure.microsoft.com/en-us/services/container-instances/#features) is
a service that allows you to spin up containerized workloads fast, without managing VMs.  This sample project
demonstrates how you use an [Azure Container Registry](https://azure.microsoft.com/en-us/services/container-registry/)
to build, publish, and eventually deploy a container to an Azure Container Instance with a TLS certificate that's
hosted on an [Azure File Share](https://docs.microsoft.com/en-us/azure/storage/files/storage-how-to-create-file-share?tabs=azure-portal).

This example makes use of an ASP.NET application running on a Linux-based container, loading the certificate data programmatically using
a simple volume mount from a File Share, as diagrammed below:

<p align="center">
  <img style="display: block; margin-left: auto; margin-right: auto; width: 60%;" src="high-level-arch.png">
</p>

> A few notes:
>
> * The .NET application assumes an RSA-signed TLS certificate.  You can modify the example [here](./api/Util/CertUtil.cs)
>   to match whichever encryption algorithm you prefer (e.g., AES instead of RSA).
> * As called out in the [Azure documentation](https://docs.microsoft.com/en-us/azure/container-instances/container-instances-volume-azure-files#limitations)
>   one specific limitation of the File Share volume method is that at the moment, it requires your container's runtime to run
>   as root.
>

## Instructions

### 1. Generate a Self-Signed TLS Certificate

If you don't yet have a TLS certificate and a private key, use the following steps to generate them.
You can do that easily with `openssl`, as demostrated below.

First, generate a new private key:

```bash
openssl req -new -newkey \
    rsa:2048 -nodes \
    -keyout aci_tls_pk.key \
    -out aci_tls_cr.csr
```

Then generate a certificate signed by the key you generated above.  Note
that you'll be prompted to enter the certificate's data:

```bash
openssl x509 -req -days 365 \
    -in aci_tls_cr.csr \
    -signkey aci_tls_pk.key \
    -out aci_tls_cert.crt
```

You should now have three new files in your current directory:

1. `aci_tls_cr.csr`: Certificate request file
1. `aci_tls_pk.key`: The RSA private key that signs your TLS certificate
1. `aci_tls_cert.crt`: The TLS certificate

### 2. Create a Blob File Share and Upload Certificate Data

[This resource](https://docs.microsoft.com/en-us/azure/container-instances/container-instances-volume-azure-files)
will guide you through the create of a Storage Account and File Share to be mounted on and used by your Container Instance.

Below you can find a summary of the steps you can take to create them:

```bash
# $ACI_STORAGE_ACCOUNT_NAME = Desired storage account name
az storage account create \
    --resource-group ${RESOURCE_GROUP} \
    --name ${ACI_STORAGE_ACCOUNT_NAME} \
    --location ${LOCATION} \
    --sku Standard_LRS
```

Next, create a Storage Share to be used by your Container Instance:

```bash
az storage share create \
  --name ${ACI_SHARE_NAME} \
  --account-name ${ACI_STORAGE_ACCOUNT_NAME}
```

Finally, upload your TLS certficate (`aci_tls_cert.crt`) and its signing private key (`aci_tls_pk.key`).
You can refer to [this guide](https://docs.microsoft.com/en-us/cli/azure/storage/file/copy?view=azure-cli-latest#az-storage-file-copy-start)
for instructions to use the Azure CLI to copy files from your host onto the Azure File Share.

### 3. Build the Image and Publish to ACR

#### 3.1 Build the Image

> **PLEASE NOTE:**
> This command assumes [Docker](https://docs.docker.com/engine/install/) is installed
> on the machine on which you're building the container image.

First, you'll have to build the container image.  You can do so locally by running the following command:

```bash
make build
```

You can verify the image has been built successfully by running `docker images`.  You should see
a line indicating that the `aci-demo` is present in your Docker daemon, e.g.:

```
REPOSITORY   TAG       IMAGE ID       CREATED         SIZE
aci-demo     latest    bb5badacc01e   8 seconds ago   104MB
```

#### 3.2 Push Image to ACR

Before you can interact with your ACR repo using your local Docker environment, you will need to
log into ACR:

```bash
# ACR_REG_NAME is a placeholder for the name of your ACR registry
az acr login --name ${ACR_REG_NAME}
```

After a successful login, you will be able to interact with the ACR registry
via your Docker daemon.

### 4. Create an ACR Service Principal

> **PLEASE NOTE:**
>
> It is highly advisable to use a **Managed Identity** instead of a **Service Principle**.
> Consider creating a **Managed Identity** for ACI to pull images from your ACR registry.
>

This section outlines the steps you should take to create a service principal that
allows Azure Container Instances (ACI) to pull images from your Azure Container Registry (ACR).

Find out in [this documentation page](https://docs.microsoft.com/en-us/azure/container-registry/container-registry-auth-aci).

### 5. Deploy the Container

> **PLEASE NOTE:**
>
> You may require to enable the ACI provider in your subscription on new subscriptions. See more information
> [here](https://docs.microsoft.com/en-us/azure/azure-resource-manager/troubleshooting/error-register-resource-provider).
>

```bash
az container create \
    --subscription ${SUBSCRIPTION_ID} \
    --resource-group ${RESOURCE_GROUP} \
    --name ${ACI_NAME} \
    --image ${REPO_NAME}/${IMAGE_NAME}:${IMAGE_TAG} \
    --registry-username ${SERVICE_PRINCIPAL_ID} \
    --registry-password ${SERVICE_PRINCIPAL_PASSWORD} \
    --dns-name-label ${ACI_NAME} \
    --ports 443 \
    --azure-file-volume-account-name ${ACI_STORAGE_ACCOUNT_NAME} \
    --azure-file-volume-account-key ${STORAGE_KEY} \
    --azure-file-volume-share-name ${ACI_SHARE_NAME} \
    --azure-file-volume-mount-path /tls-cert/
```

Let's unpack the command above:

1. The command tells the Azure CLI to pull the container's image
   from ACR, where `${REPO_NAME}/${IMAGE_NAME}:${IMAGE_TAG}` is the format
   of the full image, e.g., `acitls01.azurecr.io/aci-tls-01:latest`
1. Provide the registry credentials based on the principal ID and password
   from step 4 of the process.  Please note that if you used a Managed Identity
   (highly recommended), you can omit the `registry-username` and `registry-password`
   parameters.
1. Provide the Azure File Share credentials and the path on the container where
   the file share will be mounted.

### 6. Verify the Deployment

Should everything work as expected, you should be able to submit a `cURL` request
to the API. For instance, if the FQDN for your container instance is
`aci-tls-example.eastus.azurecontainer.io`, you can run the following command
to test the API:

```bash
# -s for a silent output 
# -k to ignore TLS verification, since our certificate is self-signed
curl -X GET \
    -s \
    -k \
   https://aci-tls-example.eastus.azurecontainer.io/weatherForecast
```

The result can look like the following JSON object:

```json
[
  {
    "date": "2022-03-23T10:05:32.330176+00:00",
    "temperatureC": 21,
    "temperatureF": 69,
    "summary": "Bracing"
  },
  {
    "date": "2022-03-24T10:05:32.3301861+00:00",
    "temperatureC": -15,
    "temperatureF": 6,
    "summary": "Sweltering"
  },
  {
    "date": "2022-03-25T10:05:32.3301866+00:00",
    "temperatureC": 45,
    "temperatureF": 112,
    "summary": "Mild"
  },
  {
    "date": "2022-03-26T10:05:32.3301867+00:00",
    "temperatureC": -4,
    "temperatureF": 25,
    "summary": "Bracing"
  },
  {
    "date": "2022-03-27T10:05:32.3301877+00:00",
    "temperatureC": -19,
    "temperatureF": -2,
    "summary": "Freezing"
  }
]
```
