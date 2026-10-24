FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY backend/Fogo.Api/Fogo.Api.csproj backend/Fogo.Api/
RUN dotnet restore backend/Fogo.Api/Fogo.Api.csproj

COPY backend/Fogo.Api/ backend/Fogo.Api/
RUN dotnet publish backend/Fogo.Api/Fogo.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
HEALTHCHECK --interval=30s --timeout=3s --start-period=20s --retries=3 CMD ["dotnet", "Fogo.Api.dll", "--healthcheck"]
USER $APP_UID
ENTRYPOINT ["dotnet", "Fogo.Api.dll"]