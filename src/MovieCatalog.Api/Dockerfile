FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY src/MovieCatalog.Domain/MovieCatalog.Domain.csproj src/MovieCatalog.Domain/
COPY src/MovieCatalog.Application/MovieCatalog.Application.csproj src/MovieCatalog.Application/
COPY src/MovieCatalog.Infrastructure/MovieCatalog.Infrastructure.csproj src/MovieCatalog.Infrastructure/
COPY src/MovieCatalog.Api/MovieCatalog.Api.csproj src/MovieCatalog.Api/
RUN dotnet restore src/MovieCatalog.Api/MovieCatalog.Api.csproj

COPY src/MovieCatalog.Domain/ src/MovieCatalog.Domain/
COPY src/MovieCatalog.Application/ src/MovieCatalog.Application/
COPY src/MovieCatalog.Infrastructure/ src/MovieCatalog.Infrastructure/
COPY src/MovieCatalog.Api/ src/MovieCatalog.Api/

RUN dotnet publish src/MovieCatalog.Api/MovieCatalog.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "MovieCatalog.Api.dll"]
