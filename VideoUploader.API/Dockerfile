# Acesse https://aka.ms/customizecontainer para saber como personalizar seu contêiner de depuração e como o Visual Studio usa este Dockerfile para criar suas imagens para uma depuração mais rápida.

# Esta fase é usada durante a execução no VS no modo rápido (Padrão para a configuração de Depuração)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base

WORKDIR /app

USER root
RUN mkdir Files && chown -R app:app Files
USER app

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

# Esta fase é usada para compilar o projeto de serviço
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["VideoUploader.API/VideoUploader.API.csproj", "VideoUploader.API/"]
COPY ["VideoUploader.Models/VideoUploader.Models.csproj", "VideoUploader.Models/"]
COPY ["VideoUploader.Services/VideoUploader.Services.csproj", "VideoUploader.Services/"]
RUN dotnet restore "./VideoUploader.API/VideoUploader.API.csproj"
COPY . .
WORKDIR "/src/VideoUploader.API"
RUN dotnet build "./VideoUploader.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Esta fase é usada para publicar o projeto de serviço a ser copiado para a fase final
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./VideoUploader.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Esta fase é usada na produção ou quando executada no VS no modo normal (padrão quando não está usando a configuração de Depuração)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "VideoUploader.API.dll"]