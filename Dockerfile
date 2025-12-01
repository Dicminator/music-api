# Imagem base de runtime (ASP.NET)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

# Imagem de build (SDK)
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copia tudo para dentro da imagem de build
COPY . .

# Restaura as dependências (vai detectar o .csproj automaticamente se só tiver um)
RUN dotnet restore

# Publica em modo Release (também detecta o projeto automaticamente)
RUN dotnet publish -c Release -o /app/publish

# Fase final: runtime + app publicado
FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .

# Porta usada no Koyeb
ENV ASPNETCORE_URLS=http://0.0.0.0:8080

ENTRYPOINT ["dotnet", "MusicApi.dll"]
