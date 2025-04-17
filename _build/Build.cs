using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using static Nuke.Common.EnvironmentInfo;
using static Nuke.Common.IO.PathConstruction;

[GitHubActions(
    "deployment",
    GitHubActionsImage.UbuntuLatest,
    OnPushBranches = new[] { "main" },
    InvokedTargets = new[] { nameof(OpenCoverageReport) }
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
                .SetReportTypes(ReportTypes.Html));
        });

    Target OpenCoverageReport => _ => _
        .DependsOn(GenerateCoverageReport)
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
}
