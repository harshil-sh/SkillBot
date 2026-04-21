# SkillBot Release Checklist

Use this checklist before every release.

## Pre-Release

### Backend (SkillBot.Api)
- [ ] All unit tests passing: `dotnet test SkillBot.Tests.Unit`
- [ ] All integration tests passing: `dotnet test SkillBot.Tests.Integration`
- [ ] No build warnings: `dotnet build SkillBot.slnx -q`
- [ ] Database migrations applied: `dotnet ef database update`
- [ ] API documentation accurate (docs/API.md)
- [ ] Version updated in appsettings.json

### Frontend (SkillBot.Web)
- [ ] Blazor builds successfully: `dotnet build SkillBot.Web`
- [ ] Production build succeeds: `dotnet publish SkillBot.Web -c Release`
- [ ] No console errors on login/chat/settings pages
- [ ] Dark mode tested
- [ ] Mobile layout tested (Chrome DevTools)
- [ ] API base URL correct for production

### Documentation
- [ ] CHANGELOG.md updated with new version
- [ ] README.md screenshots current
- [ ] All docs links working
- [ ] INSTALLATION.md accurate

## Build

- [ ] `dotnet publish SkillBot.Api -c Release` succeeds
- [ ] `dotnet publish SkillBot.Web -c Release` succeeds
- [ ] Docker image builds: `docker build -t skillbot .`
- [ ] Docker Compose starts: `docker compose up --build`
- [ ] Health check passes: `curl http://localhost:8080/health`

## Deployment

- [ ] Environment variables set (.env or secrets manager)
- [ ] Database backup taken (if production)
- [ ] Deployed to staging
- [ ] Smoke tests on staging:
  - [ ] Registration works
  - [ ] Login works
  - [ ] Chat works (with real API key)
  - [ ] Settings update works
- [ ] Security scan passed
- [ ] Performance check (Lighthouse score > 80)

## Post-Release

- [ ] GitHub Release created with tag vX.Y.Z
- [ ] CHANGELOG.md entry published
- [ ] Docker Hub image pushed (if applicable)
- [ ] Community announcement posted
- [ ] Monitoring/alerting confirmed active
- [ ] Support channels notified

## Rollback Plan

If critical issues found post-release:
1. Revert to previous Docker image tag
2. Restore database backup if schema changed
3. Update DNS/proxy to point to previous version
4. Post status update to users
5. Create hotfix branch from previous release tag
