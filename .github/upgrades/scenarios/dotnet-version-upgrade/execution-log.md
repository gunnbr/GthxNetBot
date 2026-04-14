
## [2026-04-14 10:13] 01-update-target-frameworks

All project files were updated to target .NET 10. Ambiguous LINQ Where calls in both the main data and test projects were resolved by explicitly using System.Linq.Queryable. The solution now builds successfully, completing the target framework upgrade step.

