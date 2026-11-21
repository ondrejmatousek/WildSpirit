FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY src/DesignStore.Domain/DesignStore.Domain.csproj src/DesignStore.Domain/
COPY src/DesignStore.Application/DesignStore.Application.csproj src/DesignStore.Application/
COPY src/DesignStore.Infrastructure/DesignStore.Infrastructure.csproj src/DesignStore.Infrastructure/
COPY src/DesignStore.Web/DesignStore.Web.csproj src/DesignStore.Web/
# Only the published web project is restored; test projects are not part of the image.
RUN dotnet restore src/DesignStore.Web/DesignStore.Web.csproj
COPY . .
RUN dotnet publish src/DesignStore.Web/DesignStore.Web.csproj -c Release -o /app/publish --no-restore

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "DesignStore.Web.dll"]
