FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY StudentServiceRequest.sln .
COPY src/StudentServiceRequest.Web/*.csproj ./src/StudentServiceRequest.Web/
RUN dotnet restore StudentServiceRequest.sln
COPY . .
WORKDIR /src/src/StudentServiceRequest.Web

# Set environment for EF tools to read Production config
ENV ASPNETCORE_ENVIRONMENT=Production

# Install EF Core tools
RUN dotnet tool install --global dotnet-ef
ENV PATH="$PATH:/root/.dotnet/tools"

# Run migrations against Neon (reads from appsettings.Production.json)
RUN dotnet ef database update --project /src/src/StudentServiceRequest.Web/StudentServiceRequest.Web.csproj 2>&1

# Publish
RUN dotnet publish -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "StudentServiceRequest.Web.dll"]