using System.Text.RegularExpressions;
using static Bullseye.Targets;
using static SimpleExec.Command;

const string configuration = "Release";
const string solution = "src/Typescriptr.sln";
const string packageProject = "src/Typescriptr/Typescriptr.csproj";
const string artifactsDir = "artifacts";
const string changelog = "CHANGELOG.md";

Target("clean", () =>
{
    if (Directory.Exists(artifactsDir))
        Directory.Delete(artifactsDir, recursive: true);
});

Target("restore", () => Run("dotnet", $"restore {solution}"));

Target("build", dependsOn: ["restore"], () =>
    Run("dotnet", $"build {solution} -c {configuration} --no-restore"));

Target("test", dependsOn: ["build"], () =>
    Run("dotnet", $"test {solution} -c {configuration} --no-build"));

Target("pack", dependsOn: ["test"], () =>
    Run("dotnet", $"pack {packageProject} -c {configuration} -o {artifactsDir}"));

Target("release-notes", dependsOn: ["pack"], () =>
{
    var version = ResolveVersion();
    var notes = ChangelogSection(changelog, version);
    File.WriteAllText(Path.Combine(artifactsDir, "release-notes.md"), notes);
    Console.WriteLine($"Wrote release notes for {version} to {artifactsDir}/release-notes.md");
});

Target("default", dependsOn: ["test"]);

await RunTargetsAndExitAsync(args);

// Version source of truth: the produced package. RELEASE_VERSION env overrides
// (used for local testing / to decouple from a built package).
static string ResolveVersion()
{
    var fromEnv = Environment.GetEnvironmentVariable("RELEASE_VERSION");
    if (!string.IsNullOrWhiteSpace(fromEnv))
        return fromEnv.TrimStart('v');

    var nupkg = Directory.GetFiles("artifacts", "Typescriptr.*.nupkg")
        .Select(Path.GetFileName)
        .First(f => f is not null && !f.EndsWith(".snupkg"))!;
    return Regex.Match(nupkg, @"^Typescriptr\.(.+)\.nupkg$").Groups[1].Value;
}

// Finds the changelog section whose header contains the version (prerelease
// suffixes are matched against the base version, e.g. 3.0.0-rc.1 -> 3.0.0) and
// returns the body up to the next header.
static string ChangelogSection(string path, string version)
{
    var baseVersion = version.Split('-')[0];
    var lines = File.ReadAllLines(path);
    var header = new Regex($@"^#{{1,6}}\s+\[?{Regex.Escape(baseVersion)}(\]|\s|\(|$)");
    var anyHeader = new Regex(@"^#{1,6}\s");

    var start = Array.FindIndex(lines, header.IsMatch);
    if (start < 0)
        throw new InvalidOperationException(
            $"No CHANGELOG.md section found for version {baseVersion}. Add an entry before tagging.");

    var body = new List<string>();
    for (var i = start + 1; i < lines.Length; i++)
    {
        if (anyHeader.IsMatch(lines[i])) break;
        body.Add(lines[i]);
    }

    return string.Join('\n', body).Trim() + "\n";
}
