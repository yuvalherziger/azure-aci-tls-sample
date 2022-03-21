ARG CONTAINER_REG=mcr.microsoft.com

# Override docker args to modify:
ARG PORT=443
ARG CERT_PATH=/tls-cert/aci_tls_cert.crt
ARG PK_PATH=/tls-cert/aci_tls_pk.key

FROM ${CONTAINER_REG}/dotnet/sdk:6.0-alpine as build
WORKDIR /app
COPY aci-api.sln .
COPY api/ ./api/
RUN dotnet restore
RUN dotnet publish -o /app/dist

FROM ${CONTAINER_REG}/dotnet/aspnet:6.0-alpine as runtime
WORKDIR /app
COPY --from=build /app/dist /app

RUN addgroup -S appgroup && \
    adduser -S appuser -G appgroup

USER appuser

EXPOSE ${PORT}
ENTRYPOINT ["dotnet", "/app/api.dll", "-p", ${PORT}, "-c", ${CERT_PATH}, "-k", ${PK_PATH}]
