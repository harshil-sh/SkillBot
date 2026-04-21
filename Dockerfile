FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy only the project files needed for Api — enables layer-cached restore
COPY SkillBot.Core/SkillBot.Core.csproj SkillBot.Core/
COPY SkillBot.Infrastructure/SkillBot.Infrastructure.csproj SkillBot.Infrastructure/
COPY SkillBot.Plugins/SkillBot.Plugins.csproj SkillBot.Plugins/
COPY SkillBot.Api/SkillBot.Api.csproj SkillBot.Api/
RUN dotnet restore SkillBot.Api/SkillBot.Api.csproj

# Copy source and publish
COPY SkillBot.Core/ SkillBot.Core/
COPY SkillBot.Infrastructure/ SkillBot.Infrastructure/
COPY SkillBot.Plugins/ SkillBot.Plugins/
COPY SkillBot.Api/ SkillBot.Api/
RUN dotnet publish SkillBot.Api/SkillBot.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Create non-root user and data directory
RUN useradd --no-create-home --shell /bin/false appuser \
    && mkdir -p /app/data \
    && chown -R appuser /app
USER appuser

COPY --from=build /app/publish .

EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=10s --start-period=30s --retries=3 \
  CMD curl -f http://localhost:8080/health || exit 1

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "SkillBot.Api.dll"]
