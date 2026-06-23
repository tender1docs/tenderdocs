# ---- build ----
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Restore (layer-cached on csproj changes only)
COPY TenderDocs.sln ./
COPY src/TenderDocs.Domain/TenderDocs.Domain.csproj           src/TenderDocs.Domain/
COPY src/TenderDocs.Application/TenderDocs.Application.csproj  src/TenderDocs.Application/
COPY src/TenderDocs.Infrastructure/TenderDocs.Infrastructure.csproj src/TenderDocs.Infrastructure/
COPY src/TenderDocs.Api/TenderDocs.Api.csproj                 src/TenderDocs.Api/
RUN dotnet restore src/TenderDocs.Api/TenderDocs.Api.csproj

# Build + publish
COPY . .
RUN dotnet publish src/TenderDocs.Api/TenderDocs.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

# ---- runtime ----
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

# PDF compression toolchain:
#   • ghostscript — downsamples embedded scans (image-heavy PDFs)
#   • qpdf        — lossless object-stream consolidation (vector/report PDFs)
# The compressor degrades gracefully to managed PDF optimization if either is missing.
RUN apt-get update \
    && apt-get install -y --no-install-recommends ghostscript qpdf \
    && rm -rf /var/lib/apt/lists/*

# Storage volume mount point for the LocalStorageProvider
RUN mkdir -p /app/storage
VOLUME ["/app/storage"]

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "TenderDocs.Api.dll"]
