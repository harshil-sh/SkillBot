FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and project files for layer-cached restore
COPY SkillBot.slnx .
COPY SkillBot.Core/SkillBot.Core.csproj SkillBot.Core/
COPY SkillBot.Infrastructure/SkillBot.Infrastructure.csproj SkillBot.Infrastructure/
COPY SkillBot.Plugins/SkillBot.Plugins.csproj SkillBot.Plugins/
COPY SkillBot.Api/SkillBot.Api.csproj SkillBot.Api/
RUN dotnet restore SkillBot.slnx

# Copy source and publish
COPY . .
RUN dotnet publish SkillBot.Api/SkillBot.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Create non-root user
RUN adduser --disabled-password --gecos "" appuser && chown -R appuser /app
USER appuser

# SQLite data directory
RUN mkdir -p /app/data

COPY --from=build /app/publish .

EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=10s --start-period=30s --retries=3 \
  CMD curl -f http://localhost:8080/health || exit 1

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "SkillBot.Api.dll"]
