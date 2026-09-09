# Wesal API — Git Branching & Workflow Rules

Follow these Git workflow rules for this project at all times.

## Branch Structure

This repository has ONLY TWO working branches:

- `develop` → the development branch where ALL feature development and code changes must be made.
- `main` → the production/stable branch.

## IMPORTANT: Do NOT create feature branches

When asked to implement a new feature, fix a bug, refactor code, or make any other development change:

1. Work DIRECTLY on the existing `develop` branch.
2. NEVER create a new branch such as:
   - `feature/...`
   - `fix/...`
   - `bugfix/...`
   - `hotfix/...`
   - `dev/...`
   - or ANY other additional branch.
3. Do NOT ask the user to create or switch to another branch.
4. Do NOT create an intermediate branch for any reason.

## Required Workflow

For every requested feature or code change, follow this exact workflow:

1. Check the current Git branch.
2. Make sure the current branch is `develop`.
3. If necessary, switch to the existing `develop` branch.
4. Implement the requested changes directly on `develop`.
5. Test and verify the implementation.
6. Commit the changes to `develop` with a clear commit message.
7. Push `develop` to the remote repository.
8. When the feature is complete and ready for production, create a Pull Request:
   `develop → main`
9. After the Pull Request is approved, merge `develop` into `main`.

## NEVER do this

- Do NOT use this workflow: `feature/new-feature → develop → main`
- Do NOT create `feature/...`
- Do NOT merge a feature branch into `develop`.

## ALWAYS use this workflow

`develop → main`

All development happens directly on `develop`:

```
develop
   ↓
Implement feature
   ↓
Test
   ↓
Commit
   ↓
Push
   ↓
Pull Request
   ↓
main
```

## Important Rule About main

- Never develop directly on `main`.
- Never commit feature work directly to `main`.
- `main` should only receive completed and verified changes through the Pull Request from `develop`.

## Summary

Treat the repository as a TWO-BRANCH workflow:

- `develop` = development
- `main` = production

There must be NO feature branches.