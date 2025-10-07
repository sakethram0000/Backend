FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy csproj and restore to leverage layer caching
COPY ["MyWebApi.csproj", "/app"]
RUN dotnet restore "MyWebApi.csproj"

# Copy everything else and publish
COPY *.* /app
RUN dotnet publish "MyWebApi.csproj" -c Release -o /app/publish /p:UseAppHost=false



# Use the PORT environment variable
ENV ASPNETCORE_URLS=http://+:${PORT:-8080}
EXPOSE 8080

COPY --from=build /publish .
ENTRYPOINT ["dotnet", "MyWebApi.dll"]
