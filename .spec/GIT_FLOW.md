# Git Flow Workflow Guide

This document outlines the Git Flow workflow used in the SparklerNet project to help team members follow consistent version control practices.

## Branch Strategy

We adopt the standard Git Flow branching model, which includes the following main branches:

### Permanent Branches

- **`main`**: Production environment branch that stores official release history
  - Contains only stable, released code
  - No direct development work
  - Updated by merging `release` branches
  - Each commit merged to `main` should have a version tag
  - Should always be deployable to production

- **`develop`**: Development branch containing the latest development features
  - Base branch for all new features
  - Contains all features planned for the next release
  - Integration branch for features

### Temporary Branches

- **`feature/*`**: Feature development branches
  - Created from the `develop` branch
  - Used for developing new features or improvements
  - Merged back to `develop` branch upon completion
  - Naming convention: `feature/descriptive-name`

- **`release/*`**: Release preparation branches
  - Created from the `develop` branch
  - Used for final testing, fixes, and version preparation
  - Merged to both `main` and `develop` branches upon completion
  - Naming convention: `release/x.y.z`

- **`hotfix/*`**: Emergency fix branches
  - Created from the `main` branch
  - Used for fixing critical issues in production
  - Merged to both `main` and `develop` branches upon completion
  - Naming convention: `hotfix/short-description` or `hotfix/x.y.z`

## Workflow Steps

### Initialize Project

Create the initial `develop` branch from `main` and push it to the remote repository.

### Develop New Features

1. Start from the latest `develop` branch.
2. Create a new feature branch with an appropriate name.
3. Implement the feature and commit changes.
4. Push the feature branch to the remote repository.
5. Create a Pull Request to merge the feature branch into `develop`.
6. Address any review comments and ensure CI/CD checks pass.
7. After approval, merge the Pull Request and delete the feature branch.

### Prepare a Release

1. Start from the latest `develop` branch.
2. Create a new release branch with the version number.
3. Update version numbers and perform final preparations.
4. Push the release branch to the remote repository.
5. Run final tests and make any necessary fixes.
6. Create two Pull Requests:
   - One to merge into `main`
   - One to merge back into `develop`
7. After approval, merge both Pull Requests, tag the release on the `main` branch, and delete the release branch.

### Fix Critical Issues in Production

1. Start from the latest `main` branch.
2. Create a new hotfix branch with an appropriate name or version number.
3. Fix the critical issue and commit changes.
4. Push the hotfix branch to the remote repository.
5. Create two Pull Requests:
   - One to merge into `main`
   - One to merge into `develop`
6. After expedited approval, merge both Pull Requests, tag the hotfix on the `main` branch, and delete the hotfix branch.