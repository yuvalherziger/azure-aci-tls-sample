ARG CONTAINER_REG=mcr.microsoft.com

FROM ${CONTAINER_REG}/dotnet/sdk:6.0-alpine as build
WORKDIR /app
COPY aci-api.sln .
COPY api/ ./api/
RUN dotnet restore
RUN dotnet publish -o /app/dist

FROM mcr.microsoft.com/dotnet/aspnet:6.0-alpine as runtime
WORKDIR /app
COPY --from=build /app/dist /app

RUN addgroup -S appgroup && \
    adduser -S appuser -G appgroup

USER appuser

EXPOSE 80
ENTRYPOINT ["dotnet", "/app/api.dll"]
