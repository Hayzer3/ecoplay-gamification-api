FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source
COPY . .
RUN dotnet publish src/EcoPlay.Gamification.Api/EcoPlay.Gamification.Api.csproj --configuration Release --output /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["sh", "-c", "exec dotnet EcoPlay.Gamification.Api.dll --urls http://0.0.0.0:${PORT:-8080}"]
