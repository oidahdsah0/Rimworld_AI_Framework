# Contributing to RimAI Framework

Thank you for your interest in contributing to RimAI Framework! This document provides guidelines for contributing to this open-source project.

## 🤝 **Getting Started**

### Prerequisites
- .NET Framework 4.7.2 SDK
- Visual Studio Code with C# Dev Kit
- RimWorld 1.6+ (for testing)
- Git for version control

### Development Setup
1. Fork the repository
2. Clone your fork: `git clone https://github.com/YOUR_USERNAME/Rim_AI_Framework.git`
3. Create a feature branch: `git checkout -b feature/your-feature-name`
4. Install dependencies: `dotnet restore`
5. Build the project: `dotnet build`

## 📋 **How to Contribute**

### Types of Contributions
- **Bug Reports**: Help us identify and fix issues
- **Feature Requests**: Suggest new functionality
- **Code Contributions**: Implement features or fix bugs
- **Documentation**: Improve or add documentation
- **Translations**: Add or improve language support
- **Testing**: Help test new features and releases

### Reporting Issues
Before creating an issue:
1. Check if the issue already exists
2. Provide a clear title and description
3. Include steps to reproduce
4. Attach relevant logs or screenshots
5. Specify your RimWorld version and mod list

### Pull Request Process
1. Ensure your code follows the project's coding standards
2. Include tests for new functionality
3. Update documentation as needed
4. Provide a clear description of changes
5. Link to related issues
6. Request review from maintainers

## 💻 **Coding Standards**

### Code Style
- Use C# naming conventions
- Include XML documentation for public APIs
- Follow the existing indentation and formatting
- Use meaningful variable and method names

### Example:
```csharp
/// <summary>
/// Processes a case file and generates an AI response.
/// </summary>
/// <param name="caseFile">The case to process</param>
/// <returns>AI-generated response or null if failed</returns>
public async Task<string> ProcessCaseAsync(CaseFile caseFile)
{
    if (caseFile == null)
    {
        Log.Error("RimAI Framework: Null case file provided");
        return null;
    }

    // Implementation here
}
```

### Architecture Guidelines
- Follow the Framework vs. Content separation principle
- Use dependency injection where appropriate
- Implement proper error handling and logging
- Make classes testable and mockable
- Follow SOLID principles

## 🌍 **Localization Guidelines**

### Adding New Languages
1. Create a new folder: `Languages/YourLanguage/Keyed/`
2. Copy `Settings.xml` from the English folder
3. Translate all text values (keep keys unchanged)
4. Test in-game to ensure proper display
5. Consider RTL languages special requirements

### Translation Quality
- Use appropriate technical terminology
- Maintain consistency across all strings
- Consider cultural context
- Test with actual users when possible

## 🧪 **Testing Guidelines**

### Manual Testing
- Test with a fresh RimWorld install
- Test with common mod combinations
- Verify all UI elements work correctly
- Test edge cases and error conditions

### Automated Testing
- Write unit tests for new functionality
- Ensure tests are deterministic
- Mock external dependencies (LLM APIs)
- Test both success and failure scenarios

## 📚 **Documentation Standards**

### Code Documentation
- All public APIs must have XML documentation
- Include parameter descriptions and return values
- Document exceptions that may be thrown
- Provide usage examples for complex APIs

### User Documentation
- Keep language simple and accessible
- Include screenshots where helpful
- Provide step-by-step instructions
- Update documentation with code changes

## 🔧 **Development Workflow**

### Branch Naming
- `feature/description` - New features
- `bugfix/description` - Bug fixes
- `docs/description` - Documentation changes
- `refactor/description` - Code refactoring

### Commit Messages
Use clear, descriptive commit messages:
```
feat: add streaming support for LLM responses
fix: resolve null reference in case processing
docs: update API documentation for LLMManager
refactor: simplify settings management logic
```

### Release Process
1. Feature freeze and testing period
2. Update version numbers
3. Update CHANGELOG.md
4. Create release notes
5. Tag release in Git
6. Publish to Steam Workshop

## 🎯 **Project Priorities**

### High Priority
- Core framework stability
- LLM integration reliability
- Performance optimization
- Security and privacy features

### Medium Priority
- Additional language support
- Enhanced UI/UX
- Developer tools and debugging
- Community features

### Low Priority
- Advanced customization options
- Experimental features
- Non-essential integrations

## 📞 **Getting Help**

### Communication Channels
- **GitHub Issues**: Bug reports and feature requests
- **GitHub Discussions**: General questions and community discussion
- **Discord**: Real-time chat with developers and community (coming soon)

### Code Review Process
- All code changes require review
- Maintainers will provide feedback
- Address feedback promptly
- Be open to suggestions and improvements

## 📄 **Legal Considerations**

### Licensing
- All contributions are licensed under MIT License
- Ensure you have rights to contribute code
- Don't include copyrighted material without permission
- Respect RimWorld's intellectual property

### Attribution
- Contributors will be acknowledged in releases
- Major contributors may be invited as maintainers
- Credit will be given in documentation and release notes

## 🎉 **Recognition**

We appreciate all contributions, no matter how small! Contributors will be:
- Listed in the project's contributor list
- Mentioned in release notes
- Invited to community events
- Given access to beta features

---

## 🏆 **Current Contributors**

- [@oidahdsah0](https://github.com/oidahdsah0) - Project Creator & Lead Developer

*Want to see your name here? Start contributing today!*

---

**Thank you for helping make RimAI Framework better for everyone!**
