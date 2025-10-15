# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiar arquivo de projeto e restaurar dependências
COPY src/FiapStore.Api/FiapStore.Api.csproj FiapStore.Api/
RUN dotnet restore "FiapStore.Api/FiapStore.Api.csproj"

# Copiar todo o código e compilar
COPY src/FiapStore.Api/ FiapStore.Api/
WORKDIR /src/FiapStore.Api
RUN dotnet build "FiapStore.Api.csproj" -c Release -o /app/build

# Stage 2: Publish
FROM build AS publish
RUN dotnet publish "FiapStore.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 3: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Criar usuário não-root para segurança
RUN addgroup --system --gid 1000 appuser && \
    adduser --system --uid 1000 --ingroup appuser --shell /bin/sh appuser

# Copiar binários publicados
COPY --from=publish /app/publish .

# Configurar usuário não-root
USER appuser

# Expor porta
EXPOSE 8080

# Variáveis de ambiente
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

# Comando de inicialização
ENTRYPOINT ["dotnet", "FiapStore.Api.dll"]
