FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY StudentServiceRequest.sln .
COPY src/StudentServiceRequest.Web/*.csproj ./src/StudentServiceRequest.Web/
RUN dotnet restore StudentServiceRequest.sln
COPY . .
WORKDIR /src/src/StudentServiceRequest.Web
RUN dotnet publish -c Release -o /app/publish --no-restore

# Run migrations in build stage (has SDK)
RUN dotnet ef database update --project /src/src/StudentServiceRequest.Web/StudentServiceRequest.Web.csproj --no-build 2>&1 || true

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "StudentServiceRequest.Web.dll"]