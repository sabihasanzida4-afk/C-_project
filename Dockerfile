FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY StudentServiceRequest.sln .
COPY src/StudentServiceRequest.Web/*.csproj ./src/StudentServiceRequest.Web/
RUN dotnet restore StudentServiceRequest.sln
COPY . .
WORKDIR /src/src/StudentServiceRequest.Web
RUN dotnet publish -c Release -o /app/publish --no-restore

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
RUN dotnet tool install --global dotnet-ef
ENV PATH="$PATH:/root/.dotnet/tools"
ENTRYPOINT ["sh", "-c", "dotnet ef database update 2>&1; dotnet StudentServiceRequest.Web.dll"]