FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/Wesal.Domain/Wesal.Domain.csproj src/Wesal.Domain/
COPY src/Wesal.Application/Wesal.Application.csproj src/Wesal.Application/
COPY src/Wesal.Infrastructure/Wesal.Infrastructure.csproj src/Wesal.Infrastructure/
COPY src/Wesal.Persistence/Wesal.Persistence.csproj src/Wesal.Persistence/
COPY src/Wesal.API/Wesal.API.csproj src/Wesal.API/
RUN dotnet restore src/Wesal.API/Wesal.API.csproj

COPY src/ src/
RUN dotnet publish src/Wesal.API/Wesal.API.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
RUN apt-get update && apt-get install -y --no-install-recommends libgssapi-krb5-2 && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 5000
ENV ASPNETCORE_URLS=http://0.0.0.0:5000
ENTRYPOINT ["dotnet", "Wesal.API.dll"]
