# Contributing to YurtCord

First off, thank you for considering contributing to YurtCord! 🎉

It's people like you that make YurtCord such a great tool. This document provides guidelines for contributing to the project.

## 📋 Table of Contents

- [Code of Conduct](#code-of-conduct)
- [Getting Started](#getting-started)
- [How Can I Contribute?](#how-can-i-contribute)
- [Development Workflow](#development-workflow)
- [Coding Standards](#coding-standards)
- [Commit Guidelines](#commit-guidelines)
- [Pull Request Process](#pull-request-process)
- [Community](#community)

---

## Code of Conduct

This project and everyone participating in it is governed by our Code of Conduct. By participating, you are expected to uphold this code. Please report unacceptable behavior to the maintainers.

### Our Standards

- **Be respectful** and inclusive
- **Be constructive** in criticism
- **Focus on what is best** for the community
- **Show empathy** towards other community members

---

## Getting Started

### Prerequisites

- Git
- Docker and Docker Compose
- .NET 8 SDK
- Node.js 18+
- A GitHub account

### Fork and Clone

1. **Fork the repository** on GitHub
2. **Clone your fork**:
   ```bash
   git clone https://github.com/YOUR_USERNAME/YurtCord.git
   cd YurtCord
   ```
3. **Add upstream remote**:
   ```bash
   git remote add upstream https://github.com/The404Studios/YurtCord.git
   ```

### Setup Development Environment

```bash
# Start infrastructure
docker-compose up -d postgres redis minio

# Seed database with test data
./scripts/seed-database.sh

# Terminal 1: Run backend
cd Backend/YurtCord.API
dotnet watch run

# Terminal 2: Run frontend
cd Frontend
npm install
npm run dev
```

See [DEVELOPMENT.md](DEVELOPMENT.md) for detailed setup instructions.

---

## How Can I Contribute?

### Reporting Bugs

Before creating bug reports, please check existing issues. When you create a bug report, include as many details as possible:

**Bug Report Template:**

```markdown
**Describe the bug**
A clear and concise description of what the bug is.

**To Reproduce**
Steps to reproduce the behavior:
1. Go to '...'
2. Click on '....'
3. See error

**Expected behavior**
What you expected to happen.

**Screenshots**
If applicable, add screenshots.

**Environment:**
 - OS: [e.g. Ubuntu 22.04]
 - Browser: [e.g. Chrome 120]
 - YurtCord Version: [e.g. v1.0.0]

**Additional context**
Any other relevant information.
```

### Suggesting Features

Feature requests are welcome! Please provide:

1. **Use case**: Why do you need this feature?
2. **Description**: What should the feature do?
3. **Examples**: Show examples if possible
4. **Alternatives**: Have you considered alternatives?

**Feature Request Template:**

```markdown
**Is your feature request related to a problem?**
A clear description of the problem.

**Describe the solution you'd like**
What you want to happen.

**Describe alternatives considered**
Other solutions you've thought about.

**Additional context**
Any other relevant information.
```

### Contributing Code

We love pull requests! Here's how to contribute:

1. **Find or create an issue** for what you want to work on
2. **Comment on the issue** to let others know you're working on it
3. **Fork and create a branch** from `develop`
4. **Make your changes** following our coding standards
5. **Write or update tests** for your changes
6. **Update documentation** if needed
7. **Submit a pull request**

---

## Development Workflow

### Branch Naming

Use descriptive branch names:

```
feature/add-user-profile-editing
fix/message-deletion-bug
refactor/simplify-auth-service
docs/update-api-documentation
test/add-voice-channel-tests
```

### Workflow

```bash
# 1. Sync with upstream
git fetch upstream
git checkout develop
git merge upstream/develop

# 2. Create feature branch
git checkout -b feature/your-feature-name

# 3. Make changes
# ... code, code, code ...

# 4. Run tests
cd Backend
dotnet test

cd Frontend
npm test

# 5. Commit changes
git add .
git commit -m "feat: add awesome feature"

# 6. Push to your fork
git push origin feature/your-feature-name

# 7. Create Pull Request on GitHub
```

---

## Coding Standards

### Backend (C#)

**Style:**
- Use **C# 12** features (primary constructors, pattern matching)
- Follow **Clean Architecture** principles
- All I/O operations must be **async**
- Use **nullable reference types**

**Naming:**
- `PascalCase` for classes, methods, properties
- `camelCase` for parameters, local variables
- `_camelCase` for private fields
- Prefix interfaces with `I`

**Example:**

```csharp
public class UserService(IUserRepository repository) : IUserService
{
    private readonly IUserRepository _repository = repository;

    /// <summary>
    /// Gets a user by their unique identifier
    /// </summary>
    /// <param name="userId">The user's snowflake ID</param>
    /// <returns>User object or null if not found</returns>
    public async Task<User?> GetUserAsync(Snowflake userId)
    {
        return await _repository.GetByIdAsync(userId);
    }
}
```

**Guidelines:**
- ✅ XML documentation for public APIs
- ✅ Use `var` for obvious types
- ✅ Early returns over nested ifs
- ✅ LINQ for collections
- ✅ String interpolation
- ❌ Magic numbers/strings
- ❌ Swallowing exceptions

### Frontend (TypeScript)

**Style:**
- Use **TypeScript** with strict mode
- **Functional components** with hooks
- **Redux Toolkit** for state management

**Naming:**
- `PascalCase` for components
- `camelCase` for functions, variables
- `UPPER_CASE` for constants

**Example:**

```typescript
interface MessageProps {
  message: Message;
  onDelete: (id: string) => void;
}

export const MessageComponent: React.FC<MessageProps> = ({
  message,
  onDelete
}) => {
  const handleDelete = () => {
    onDelete(message.id);
  };

  return (
    <div className="message">
      <span>{message.content}</span>
      <button onClick={handleDelete}>Delete</button>
    </div>
  );
};
```

**Guidelines:**
- ✅ TypeScript interfaces for props
- ✅ Functional components
- ✅ React hooks
- ✅ Memoization when needed
- ✅ Proper prop-types
- ❌ `any` type
- ❌ Class components
- ❌ Inline styles (use CSS modules)

---

## Commit Guidelines

We follow **Conventional Commits** specification:

### Format

```
<type>(<scope>): <description>

[optional body]

[optional footer]
```

### Types

- `feat`: New feature
- `fix`: Bug fix
- `docs`: Documentation only
- `style`: Code style (formatting, missing semicolons)
- `refactor`: Code change that neither fixes bug nor adds feature
- `perf`: Performance improvement
- `test`: Adding missing tests
- `chore`: Changes to build process or tools

### Examples

```bash
# Feature
feat(auth): add password reset functionality

# Bug fix
fix(messages): resolve race condition in message sending

# Documentation
docs(api): update authentication endpoint examples

# Refactoring
refactor(voice): simplify WebRTC connection logic

# Multiple paragraphs
feat(guilds): add role hierarchy management

Implements role drag-and-drop reordering and automatic
permission inheritance based on position.

Closes #123
```

### Rules

- ✅ Use imperative mood ("add" not "added")
- ✅ Don't capitalize first letter
- ✅ No period at the end
- ✅ Limit first line to 72 characters
- ✅ Reference issues in footer

---

## Pull Request Process

### Before Submitting

**Checklist:**

- [ ] Code follows project style guidelines
- [ ] Self-review completed
- [ ] Comments added for complex code
- [ ] Documentation updated
- [ ] Tests added/updated
- [ ] All tests pass locally
- [ ] No new warnings introduced
- [ ] Commit messages follow guidelines

### PR Template

```markdown
## Description
Brief description of changes

## Type of Change
- [ ] Bug fix
- [ ] New feature
- [ ] Breaking change
- [ ] Documentation update

## Testing
How has this been tested?

## Screenshots (if applicable)
Add screenshots for UI changes

## Checklist
- [ ] Code follows style guidelines
- [ ] Self-reviewed
- [ ] Commented complex code
- [ ] Updated documentation
- [ ] Added tests
- [ ] Tests pass
- [ ] No new warnings

## Related Issues
Closes #(issue number)
```

### Review Process

1. **Automated checks** must pass (CI/CD)
2. **At least one approval** from maintainers
3. **All conversations resolved**
4. **Up to date** with base branch

### After Approval

- We'll merge using **squash and merge** for clean history
- Your contribution will be in the next release! 🎉

---

## Testing

### Backend Tests

```bash
cd Backend

# Run all tests
dotnet test

# Run specific test
dotnet test --filter "ClassName.MethodName"

# With coverage
dotnet test /p:CollectCoverage=true
```

### Frontend Tests

```bash
cd Frontend

# Run all tests
npm test

# Run in watch mode
npm run test:watch

# Coverage report
npm run test:coverage
```

### Writing Tests

**Backend:**
```csharp
[Fact]
public async Task GetUserAsync_ValidId_ReturnsUser()
{
    // Arrange
    var userId = new Snowflake(123456789);

    // Act
    var user = await _service.GetUserAsync(userId);

    // Assert
    Assert.NotNull(user);
    Assert.Equal(userId, user.Id);
}
```

**Frontend:**
```typescript
describe('MessageComponent', () => {
  it('renders message content', () => {
    const message = { id: '1', content: 'Hello' };
    render(<MessageComponent message={message} />);

    expect(screen.getByText('Hello')).toBeInTheDocument();
  });
});
```

---

## Documentation

### Code Documentation

- **Backend**: XML comments for public APIs
- **Frontend**: JSDoc comments for components
- **README**: Update if adding features

### API Documentation

Update [API_DOCUMENTATION.md](API_DOCUMENTATION.md) when:
- Adding new endpoints
- Changing request/response formats
- Modifying authentication

---

## Community

### Communication Channels

- **GitHub Issues**: Bug reports and feature requests
- **GitHub Discussions**: Questions and general discussion
- **Pull Requests**: Code reviews and discussions

### Getting Help

- Check [DEVELOPMENT.md](DEVELOPMENT.md) for setup help
- Search existing issues and discussions
- Ask questions in GitHub Discussions
- Be patient and respectful

---

## Recognition

Contributors will be:
- Listed in our README
- Credited in release notes
- Part of our growing community! 🌟

---

## License

By contributing, you agree that your contributions will be licensed under the MIT License.

---

## Questions?

Feel free to ask questions in:
- **GitHub Discussions**
- **Issue comments**
- **Pull request comments**

We're here to help! 🚀

---

**Thank you for contributing to YurtCord!** ❤️

<div align="center">

[⬆ Back to Top](#contributing-to-yurtcord)

</div>
