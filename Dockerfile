# Imagem base de runtime (ASP.NET)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

# Imagem de build (SDK)
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copia tudo pro container de build
COPY . .

# Restaura dependências
RUN dotnet restore "./MusicApi.csproj"

# Publica em modo Release
RUN dotnet publish "./MusicApi.csproj" -c Release -o /app/publish

# Fase final: só o runtime + app publicado
FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .

# Porta padrão pro Koyeb
ENV ASPNETCORE_URLS=http://0.0.0.0:8080

ENTRYPOINT ["dotnet", "MusicApi.dll"]
