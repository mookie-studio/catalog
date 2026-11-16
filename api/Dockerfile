# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY src/Catalog.Api/Catalog.Api.csproj src/Catalog.Api/
RUN dotnet restore src/Catalog.Api/Catalog.Api.csproj
COPY src/ src/
# The OpenAPI document is generated and committed by developers; the image does not need it.
RUN dotnet publish src/Catalog.Api/Catalog.Api.csproj -c Release -o /app --no-restore -p:OpenApiGenerateDocuments=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
# APP_UID is the non-root user that the official .NET images provide.
USER $APP_UID
ENTRYPOINT ["dotnet", "Catalog.Api.dll"]
