FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY src/NotificationConsumer.Application/NotificationConsumer.Application.csproj src/NotificationConsumer.Application/
COPY src/NotificationConsumer.Infrastructure/NotificationConsumer.Infrastructure.csproj src/NotificationConsumer.Infrastructure/
COPY src/NotificationConsumer.Api/NotificationConsumer.Api.csproj src/NotificationConsumer.Api/
RUN dotnet restore src/NotificationConsumer.Api/NotificationConsumer.Api.csproj

COPY src/NotificationConsumer.Application/ src/NotificationConsumer.Application/
COPY src/NotificationConsumer.Infrastructure/ src/NotificationConsumer.Infrastructure/
COPY src/NotificationConsumer.Api/ src/NotificationConsumer.Api/

RUN dotnet publish src/NotificationConsumer.Api/NotificationConsumer.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "NotificationConsumer.Api.dll"]
