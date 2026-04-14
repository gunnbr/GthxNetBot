# .NET Version Upgrade

## Strategy
Sequential upgrade: frameworks → packages → DI refactor → compatibility fixes → validation.

## Preferences
- **Flow Mode**: Automatic
- **Commit Strategy**: After Each Task
- **Pace**: Standard
- **Target Framework**: net10.0
- **Working Branch**: upgrade-to-NET10

## Decisions
- Upgrade all projects to .NET 10 (LTS)
- Refactor console app to use Generic Host DI

## Custom Instructions
