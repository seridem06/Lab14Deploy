FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY Lab14Api/Lab14Api.csproj Lab14Api/
RUN dotnet restore Lab14Api/Lab14Api.csproj

COPY . .
RUN dotnet publish Lab14Api/Lab14Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Lab14Api.dll"]
