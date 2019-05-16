#addin "Cake.Docker&version=0.10.0"

var target = Argument<string>("target", "Default");

var subdomainName = MakeAbsolute(new DirectoryPath(".")).GetDirectoryName();
var dockerFiles = GetFiles("./**/Dockerfile").Select(file => file.ToString());

Task("Clean")
    .Description("Cleans all directories that are used during the build process.")
    .Does(() =>
{
    DotNetCoreClean(".");
});

Task("Restore")
    .Description("Restores all the NuGet packages that are used by the specified solution.")
    .Does(() =>
{
    DotNetCoreRestore(".");
});

Task("Build")
    .Description("Builds all the different parts of the project.")
    .IsDependentOn("Restore")
    .Does(() =>
{
    DotNetCoreBuild(".");
});

Task("DockerBuild")
    .Description("Builds all the different parts of the project, including the building of relevant Docker images.")
    .IsDependentOn("Build")
    .Does(() =>
{
    foreach(var dockerFile in dockerFiles)
    {
        var applicationName = new FilePath(dockerFile).GetDirectory().GetDirectoryName().ToLower();
        Information("Docker Building {0} - subdomain: {1}; application name: {2}", dockerFile, subdomainName, applicationName);
        DockerBuild(new DockerImageBuildSettings{File = dockerFile, ForceRm = true, Tag = new[]{ $"{subdomainName.ToLower()}.{applicationName.ToLower()}:latest" }}, ".");
    }
});

Task("Test")
    .Description("Executes specifications and tests not categorized as 'Slow'.")
    .IsDependentOn("Build")
    .Does(() =>
{
    var filter = "Category!=Slow";
    DotNetCoreTest("Specification", new DotNetCoreTestSettings{ Filter = filter });
    DotNetCoreTest("Test", new DotNetCoreTestSettings{ Filter = filter });
});

Task("TestSlow")
    .Description("Executes all specifications and tests, including those categorized as 'Slow'.")
    .IsDependentOn("Build")
    .Does(() =>
{
    DotNetCoreTest("Specification");
    DotNetCoreTest("Test");
});

Task("Default")
    .Description("This is the default task which will be run if no specific target is passed in - defaults to Test.")
    .IsDependentOn("Test");

RunTarget(target);
