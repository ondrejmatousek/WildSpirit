FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY DesignStore.slnx ./
COPY src/DesignStore.Domain/DesignStore.Domain.csproj src/DesignStore.Domain/
COPY src/DesignStore.Application/DesignStore.Application.csproj src/DesignStore.Application/
COPY src/DesignStore.Infrastructure/DesignStore.Infrastructure.csproj src/DesignStore.Infrastructure/
COPY src/DesignStore.Web/DesignStore.Web.csproj src/DesignStore.Web/
COPY tests/DesignStore.Tests/DesignStore.Tests.csproj tests/DesignStore.Tests/
RUN dotnet restore DesignStore.slnx
COPY . .
RUN dotnet publish src/DesignStore.Web/DesignStore.Web.csproj -c Release -o /app/publish --no-restore

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "DesignStore.Web.dll"]
