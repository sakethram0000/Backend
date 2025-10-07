FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /

# Copy csproj and restore to leverage layer caching
COPY ["MyWebApi.csproj", "./"]
RUN dotnet restore "MyWebApi.csproj"

# Copy everything else and publish
COPY . .
RUN dotnet publish "MyWebApi.csproj" -c Release -o /publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /

# Use the PORT Vercel supplies
ENV ASPNETCORE_URLS=http://+:${PORT}
EXPOSE 80

COPY --from=build /publish .
ENTRYPOINT ["dotnet", "MyWebApi.dll"]
