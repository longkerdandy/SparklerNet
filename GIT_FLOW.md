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

- **`bugfix/*`**: Bug fix branches (optional)
  - Created from the `develop` branch
  - Used for fixing non-critical bugs that don't require hotfixes
  - Merged back to `develop` branch upon completion
  - Naming convention: `bugfix/descriptive-name`

## Workflow Steps

### Initialize Project (Already Completed)

```bash
# Switch to main branch
git checkout main

# Create develop branch
git checkout -b develop

# Push develop branch to remote repository
git push -u origin develop
```

### Develop New Features

```bash
# Ensure you start from the latest develop branch
git checkout develop
git pull origin develop

# Create a new feature branch
git checkout -b feature/my-new-feature

# After development is complete, commit changes
git add .
git commit -m "feat: Implement my new feature"

# Push feature branch to remote repository
git push -u origin feature/my-new-feature

# Create a Pull Request for code review
# After code review and approval, merge to develop branch
git checkout develop
git pull origin develop
git merge --no-ff feature/my-new-feature

# Push merged develop to remote
git push origin develop

# Delete local feature branch
git branch -d feature/my-new-feature

# Delete remote feature branch
git push origin --delete feature/my-new-feature
```

### Prepare a Release

```bash
# Ensure develop branch is up to date
git checkout develop
git pull origin develop

# Create release branch
git checkout -b release/1.0.0

# Update version numbers and perform necessary preparation
git add .
git commit -m "chore: Bump version to 1.0.0"

# Push release branch to remote repository
git push -u origin release/1.0.0

# Run final tests and fix any critical issues
# (Make commits as needed for fixes)

# After release preparation is complete, merge to main branch
git checkout main
git pull origin main
git merge --no-ff release/1.0.0

# Tag the release
git tag -a v1.0.0 -m "Release version 1.0.0"

# Push the tag to remote repository
git push origin v1.0.0

# Push the updated main branch
git push origin main

# Also merge the release branch back to develop branch
git checkout develop
git pull origin develop
git merge --no-ff release/1.0.0

# Push merged develop to remote
git push origin develop

# Delete release branch
git branch -d release/1.0.0
git push origin --delete release/1.0.0
```

### Fix Critical Issues in Production

```bash
# Create hotfix branch from main branch
git checkout main
git pull origin main
git checkout -b hotfix/1.0.1

# After fixing the issue, commit changes
git add .
git commit -m "fix: Resolve critical production issue"

# Push hotfix branch to remote for review
git push -u origin hotfix/1.0.1

# After review, merge back to main branch
git checkout main
git merge --no-ff hotfix/1.0.1

# Tag the hotfix
git tag -a v1.0.1 -m "Hotfix version 1.0.1"

# Push the tag to remote repository
git push origin v1.0.1

# Push the updated main branch
git push origin main

# Also merge the hotfix back to develop branch
git checkout develop
git pull origin develop
git merge --no-ff hotfix/1.0.1

# Push merged develop to remote
git push origin develop

# Delete hotfix branch
git branch -d hotfix/1.0.1
git push origin --delete hotfix/1.0.1
```

## Best Practices

### Branch Management

1. **Branch Naming**:
   - Use clear, descriptive branch names
   - Follow consistent naming conventions
   - Example formats:
     - `feature/user-authentication`
     - `release/2.1.0`
     - `hotfix/fix-memory-leak`
     - `bugfix/correct-api-endpoint`

2. **Branch Lifecycle**:
   - Create branches only when needed
   - Keep feature branches small and focused
   - Delete branches after they've been merged
   - Avoid long-lived branches to prevent merge conflicts

### Commit Guidelines

3. **Commit Messages**:
   - Write meaningful commit messages following this format:
   ```
   <type>: <description>
   
   <detailed description (optional)>
   ```
   - Types:
     - `feat`: New feature
     - `fix`: Bug fix
     - `docs`: Documentation changes
     - `style`: Code style changes (formatting, etc.)
     - `refactor`: Code changes that neither fix a bug nor add a feature
     - `test`: Adding or updating tests
     - `chore`: Changes to build process or auxiliary tools
   - Keep the first line short (50 chars or less)
   - Use imperative mood ("Add feature" not "Added feature")
   - Reference issue numbers when applicable

4. **Commit Frequency**:
   - Commit early and often
   - Each commit should represent a logical unit of work
   - Avoid large, monolithic commits

### Collaboration Practices

5. **Code Reviews**:
   - All code should be reviewed via Pull Requests before merging
   - At least one approval should be required
   - Discuss and resolve all comments before merging

6. **Synchronization**:
   - Pull the latest changes from remote repositories regularly
   - Fetch and merge changes from `develop` into long-running feature branches to avoid large merge conflicts
   - Communicate with team members about ongoing work

7. **Continuous Integration**:
   - Run tests locally before pushing code
   - Ensure all CI checks pass before merging to `develop` or `main`

### Release Management

8. **Versioning**:
   - Follow semantic versioning: MAJOR.MINOR.PATCH
   - MAJOR: Incompatible API changes
   - MINOR: Backward-compatible new features
   - PATCH: Backward-compatible bug fixes
   - Update version numbers in the appropriate files

9. **Release Documentation**:
   - Maintain a changelog documenting changes in each release
   - Include version numbers, dates, and summaries of changes

## Special Cases and Tips

1. **Handling Merge Conflicts**:
   - Address conflicts promptly
   - Understand the changes causing conflicts
   - Test thoroughly after resolving conflicts

2. **Working with Remote Teams**:
   - Communicate branch usage and intentions
   - Use branch protection rules to enforce workflow
   - Consider time zone differences when planning merges

3. **Recovering from Mistakes**:
   - Use `git revert` for undoing public changes
   - Be cautious with `git reset` on shared branches
   - Consult with team members before force-pushing

4. **Feature Flags**:
   - Consider using feature flags for larger changes
   - This allows merging incomplete features to `develop` without affecting functionality

5. **Document Updates**:
   - Update this document as workflow evolves
   - Ensure all team members are aware of changes to the process

---

Following this Git Flow workflow will help us better manage code versions, improve development efficiency, and ensure code quality throughout the SparklerNet project lifecycle.