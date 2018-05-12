FROM microsoft/aspnetcore:2.0-nanoserver-1709 AS base
WORKDIR /app
EXPOSE 80

FROM microsoft/aspnetcore-build:2.0-nanoserver-1709 AS build
WORKDIR /src
COPY NZSmartIDReader.Api/NZSmartIDReader.Api.csproj NZSmartIDReader.Api/
RUN dotnet restore NZSmartIDReader.Api/NZSmartIDReader.Api.csproj
COPY . .
WORKDIR /src/NZSmartIDReader.Api
RUN dotnet build NZSmartIDReader.Api.csproj -c Release -o /app

FROM build AS publish
RUN dotnet publish NZSmartIDReader.Api.csproj -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "NZSmartIDReader.Api.dll"]
