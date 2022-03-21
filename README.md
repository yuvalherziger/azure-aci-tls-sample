# aci-api

This project includes an example of an ASP.NET based REST API
deployed as an [Azure Container Instance (ACI)](https://docs.microsoft.com/en-us/azure/container-instances/)
with TLS support, with the TLS certificates hosted in an
[Azure File Share](https://docs.microsoft.com/en-us/azure/storage/files/storage-how-to-create-file-share?tabs=azure-portal).

## Instructions

## 1. (Optional) Generate a Self-Signed TLS Certificate

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

## 2. Build the Image and Publish to ACR

## TBA:

```bash
az container create \
    --resource-group yherziger-vms \
    --name aci-tls-demo-01 \
    --image yherzigeracr.azurecr.io/aci-demo:latest \
    --registry-username <service-principal-ID> \
    --registry-password <service-principal-password> \
    --dns-name-label aci-tls-demo-01 \
    --ports 443 \
    --azure-file-volume-account-name acitlsdemo01 \
    --azure-file-volume-account-key $STORAGE_KEY \
    --azure-file-volume-share-name acitlsdemo01storageshare \
    --azure-file-volume-mount-path /tls-cert/
```
