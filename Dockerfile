FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY EpamPreOrderApi/EpamPreOrderApi.csproj EpamPreOrderApi/
RUN dotnet restore EpamPreOrderApi/EpamPreOrderApi.csproj

COPY EpamPreOrderApi/ EpamPreOrderApi/
WORKDIR /src/EpamPreOrderApi
RUN dotnet publish EpamPreOrderApi.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish ./

ENV ASPNETCORE_URLS=http://0.0.0.0:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "EpamPreOrderApi.dll"]
