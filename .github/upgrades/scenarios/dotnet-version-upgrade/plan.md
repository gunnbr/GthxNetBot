# .NET Version Upgrade Plan

## Overview

**Target**: Upgrade all projects in the solution to .NET 10 and refactor the main console application to use the Generic Host pattern for dependency injection.
**Scope**: 7 projects, moderate complexity, includes DI refactor for console app and package updates.

## Tasks

### 01-update-target-frameworks

Update the target framework for all projects from .NET 6 to .NET 10. This ensures compatibility with the latest .NET features and long-term support.

**Done when**: All project files target net10.0 and build successfully.

---

### 02-update-nuget-packages

Upgrade all NuGet packages to the latest compatible versions, replacing deprecated packages as needed. This addresses security, compatibility, and support issues flagged in the assessment.

**Done when**: No deprecated or outdated packages remain, and all projects restore successfully.

---

### 03-refactor-console-app-di

Refactor the main console application to use the Generic Host pattern for dependency injection, configuration, and logging. Remove any legacy ASP.NET or ad-hoc DI patterns, ensuring a clean, modern setup.

**Done when**: The console app starts via Generic Host, all services are registered and resolved via DI, and the app runs as expected.

---

### 04-address-api-incompatibilities

Resolve any source or behavioral incompatibilities identified in the test projects or elsewhere due to the .NET 10 upgrade. Update code to use supported APIs and adjust for breaking changes.

**Done when**: All code compiles, tests pass, and no upgrade-related runtime errors remain.

---

### 05-validation-and-testing

Run all unit and integration tests, validate application behavior, and perform manual smoke testing. Ensure the upgraded solution is stable and ready for production use.

**Done when**: All tests pass and the application functions as expected in .NET 10.
