FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore to leverage layer caching
COPY ["MyWebApi.csproj", "./"]
RUN dotnet restore "MyWebApi.csproj"

# Copy everything else and publish
COPY . .
RUN dotnet publish "MyWebApi.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Use the PORT environment variable
ENV ASPNETCORE_URLS=http://+:${PORT:-80}
EXPOSE 80

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "MyWebApi.dll"]
