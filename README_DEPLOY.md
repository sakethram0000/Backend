# Deploying MyWebApi to Vercel (Docker-based)

This document explains how to deploy the `MyWebApi` ASP.NET Core 8 application to Vercel using the Docker builder. Vercel does not natively run .NET apps — the Docker builder will build and run the container.

Files added to help deployment:
- `Dockerfile` — multi-stage Dockerfile to build and run the app.
- `.dockerignore` — files excluded from the Docker build context.
- `vercel.json` — instructs Vercel to use the Docker builder.

Environment variables (set these in the Vercel Project → Settings → Environment Variables):

- `ConnectionStrings__DefaultConnection` — full SQL Server connection string used by EF Core.
- `Jwt__SecretKey`
- `Jwt__Issuer`
- `Jwt__Audience`
- `Jwt__ExpirationMinutes`
- `ASPNETCORE_ENVIRONMENT` — set to `Production` in Vercel for production runs.

Notes:
- Vercel provides a `PORT` environment variable at runtime; the `Dockerfile` sets `ASPNETCORE_URLS` to listen on that port.
- Vercel will build the image using the `Dockerfile` at the `MyWebApi` folder root. When you import the repo into Vercel, set the Project Root to `MyWebApi` so Vercel finds `vercel.json` and `Dockerfile`.
- Vercel does not host SQL Server. Use an external DB (Azure SQL, AWS RDS SQL Server, or another host) and make sure the app can reach it (open firewalls, allow public access, or use VPC/static IP allow lists as required).
- If your app runs EF migrations on startup, ensure migrations are safe for production, or run them manually using the `dotnet ef database update` CLI against the target DB before deploying.

How to deploy
1. Commit and push these files to your repository.
2. In Vercel: Create a new project and import the repository.
   - During import set the Project Root to `MyWebApi`.
3. In Project Settings → Environment Variables, add the variables listed above.
4. Deploy. Vercel will use `@vercel/docker` to build your `Dockerfile` and run your app.

Troubleshooting
- Build fails: check the Vercel build logs. Common issues: missing SDK, missing csproj in context, or long build times.
- App cannot connect to DB: confirm connection string, DB firewall, and that the DB accepts connections from Vercel's outbound IPs.
- If you need to persist files, use cloud storage (Azure Blob or S3) since containers are ephemeral.

Optional: Local build/test commands

PowerShell (Windows):

```powershell
cd MyWebApi
dotnet restore
dotnet publish -c Release -o publish /p:UseAppHost=false
docker build -t mywebapi:local .
docker run -e PORT=80 -p 8080:80 mywebapi:local
# open http://localhost:8080
```
