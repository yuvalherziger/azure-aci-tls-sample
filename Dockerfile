ARG CONTAINER_REG=mcr.microsoft.com

# Override docker args to modify:

FROM ${CONTAINER_REG}/dotnet/sdk:6.0-alpine as build
WORKDIR /app
COPY aci-api.sln .
COPY api/ ./api/
RUN dotnet restore
RUN dotnet publish -o /app/dist

FROM ${CONTAINER_REG}/dotnet/aspnet:6.0-alpine as runtime
# Override docker args to modify:
ARG PORT=443
ARG CERT_PATH=/tls-cert/aci_tls_cert.crt
ARG PK_PATH=/tls-cert/aci_tls_pk.key
ENV PORT ${PORT}
ENV CERT_PATH ${CERT_PATH}
ENV PK_PATH ${PK_PATH}

WORKDIR /app
COPY --from=build /app/dist /app
COPY entrypoint.sh .

# If you don't intend to mount an Azure file share for
# the TLS data, it's highly advised that you uncomment
# the following block to avoid running the container as
# root:

# ----------
# RUN addgroup -S appgroup && \
#     adduser -S appuser -G appgroup

# USER appuser
# ----------

EXPOSE ${PORT}

ENTRYPOINT ["/bin/sh", "-c", "dotnet /app/api.dll -p ${PORT} -c ${CERT_PATH} -k ${PK_PATH}"]
