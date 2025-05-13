using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Nuke.Common;
using Nuke.Common.CI;
using Nuke.Common.CI.GitHubActions;
using Nuke.Common.CI.GitHubActions.Configuration;
using Nuke.Common.Execution;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.Coverlet;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.ReportGenerator;
using Nuke.Common.Utilities.Collections;
using Serilog;
using static Nuke.Common.EnvironmentInfo;
using static Nuke.Common.IO.PathConstruction;

[GitHubActions(
    "deployment",
    GitHubActionsImage.UbuntuLatest,
    OnPushBranches = new[] { "main" },
    InvokedTargets = new[] { nameof(PublishCoverageReport) }
)]

class Build : NukeBuild
{
    public static int Main() => Execute<Build>(x => x.OpenCoverageReport);

    [Solution] readonly Solution Solution;

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

    AbsolutePath CoverageTestResultsPath => RootDirectory / "TestResults";
    AbsolutePath CoverageReportPath => RootDirectory / "CoverageReport";

    Target CleanTestResults => _ => _
        .Executes(() =>
        {
            (CoverageTestResultsPath).DeleteDirectory();
            (CoverageReportPath).DeleteDirectory();
        });

    Target Clean => _ => _
        .DependsOn(CleanTestResults)
        .Executes(() =>
        {
            DotNetTasks.DotNetClean(x => x
                .SetConfiguration(Configuration)
                .SetProject(Solution));
        });

    Target Restore => _ => _
        .DependsOn(Clean)
        .Executes(() =>
        {
            DotNetTasks.DotNetRestore(x => x
                .SetProjectFile(Solution));
        });

    Target BuildSolution => _ => _
        .DependsOn(Restore)
        .Executes(() =>
        {
            DotNetTasks.DotNetBuild(x => x
                .SetProjectFile(Solution)
                .SetNoRestore(true));
        });

    Target TestSolution => _ => _
        .DependsOn(BuildSolution)
        .Executes(() =>
        {
            DotNetTasks.DotNetTest(x => x
                .SetProjectFile(Solution)
                .SetNoRestore(true)
                .SetNoBuild(true)
                .SetCollectCoverage(true)
                .SetSettingsFile(RootDirectory / "src" / "tests.runsettings")
                .SetResultsDirectory(CoverageTestResultsPath));
        });

    Target GenerateCoverageReport => _ => _
        .DependsOn(TestSolution)
        .Executes(() =>
        {
            ReportGeneratorTasks.ReportGenerator(_ => _
                .SetReports(CoverageTestResultsPath / "*" / "coverage.cobertura.xml")
                .SetTargetDirectory(CoverageReportPath)
                .SetReportTypes([ReportTypes.MarkdownSummaryGithub,
                    ReportTypes.HtmlInline,
                    ReportTypes.Badges]));
        });

    Target OpenCoverageReport => _ => _
        .DependsOn(GenerateCoverageReport)
        .OnlyWhenStatic(() => Host is not GitHubActions)
        .Executes(() =>
        {
            var reportFile = CoverageReportPath / "index.html";
            Assert.FileExists(reportFile, $"Coverage report not found at {reportFile}");

            Process.Start(new ProcessStartInfo
            {
                FileName = reportFile,
                UseShellExecute = true
            });
        });

    Target PublishCoverageReport => _ => _
        .DependsOn(GenerateCoverageReport)
        .OnlyWhenStatic(() => Host is GitHubActions)
        .Produces(CoverageReportPath / "*.html")
        .Executes(() =>
        {
            // Path to the Markdown summary report
            var markdownReportPath = CoverageReportPath / "SummaryGithub.md"; // Or "Summary.md"
            Assert.FileExists(markdownReportPath, $"Markdown summary report not found at {markdownReportPath}");

            // Path to the coverage badge
            var badgePath = CoverageReportPath / "badge_combined.svg";
            Assert.FileExists(badgePath, $"Coverage badge not found at {badgePath}");

            // Read the Markdown report content
            var markdownContent = File.ReadAllText(markdownReportPath);

            // Append to $GITHUB_STEP_SUMMARY
            var summaryFile = Environment.GetEnvironmentVariable("GITHUB_STEP_SUMMARY");
            Assert.NotNull(summaryFile, "$GITHUB_STEP_SUMMARY environment variable is not set.");
            File.AppendAllText(summaryFile, $"# Code Coverage Report\n\n{markdownContent}");
            Log.Information("Appended Markdown summary to GitHub Actions summary.");

            // Copy badge to a docs/coverage directory for committing
            var badgeOutputPath = RootDirectory / "docs" / "badge_combined.svg";
            badgeOutputPath.Parent.CreateDirectory();
            File.Copy(badgePath, badgeOutputPath, overwrite: true);
            Log.Information("Copied badge to {Path} for committing.", badgeOutputPath);

            // Upload the coverage report as a GitHub Actions artifact
            var artifactPath = CoverageReportPath;
            Assert.DirectoryExists(artifactPath, $"Coverage report directory not found at {artifactPath}");
            Log.Information($"Uploading coverage report from {artifactPath} as artifact...");
        });
}
