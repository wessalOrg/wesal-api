# Deployment

## Branch workflow
- Only `develop` and `main` are used. All code flows `develop` -> `main`.
- No feature/fix/hotfix branches.

## Target
- Production API: https://wesal-api.apps.taqat.academy
- Source repository: wessalOrg/wesal-api
- Platform: Taqat (Dokku). Build: Nixpacks. Runtime: dotnet 10 (Procfile `web` start).

## Deployment mechanism
- A GitHub push to `main` is delivered to Taqat via the push webhook (id 676592697).
- Taqat clones the repository to /tmp/deploy-wesal-api, produces a tarball
  (/tmp/wesal-api.tar.gz), and deploys it through:
      dokku git:from-archive wesal-api
- The archive intentionally excludes the .git directory, so the deployed
  revision is derived from file contents and GIT_REV is a Dokku-generated
  commit id that will not match a GitHub SHA.
- A trigger/empty commit therefore logs
      No changes detected, skipping git commit
  because the file content is unchanged from the previously deployed archive.

## Known Dokku host issue
- Dokku < 0.36.0 git-from-archive plugin may log:
      mv: cannot move '.' ... Device or resource busy
  Root cause: `find . -maxdepth 1` passes `.` to `mv` while `.` is the process
  current working directory (rename(2) returns EBUSY). Upstream fix:
  dokku/dokku PR #7811 (commit fb405c6, shipped in v0.36.0) changes the command
  to `find . -mindepth 1 -maxdepth 1`. On this host the message is non-fatal.

## Verification
- Health: curl -s https://wesal-api.apps.taqat.academy/health/live
- In-container env: GIT_REV / PORT / ASPNETCORE_URLS
- Owner routes require authentication (HTTP 401 without a Bearer token).