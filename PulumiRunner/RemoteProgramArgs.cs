﻿using LibGit2Sharp;
using LibGit2Sharp.Handlers;
using Pulumi;
using Pulumi.Automation;
using Serilog;
using System;
using System.Diagnostics;
using System.Text;
using YamlDotNet.Serialization;
using static Katasec.PulumiRunner.RemoteProgram;

namespace Katasec.PulumiRunner;

public class RemoteProgram
{

    public string WorkDir { get; }
    public WorkspaceStack Stack { get; }
    public string ConfigFile {
        get {
            return Path.Join(WorkDir, $"pulumi.{_stackName}.yaml");
        }
    }

    public ProjectConfig ProjectConfig { get; }

    public string ProjectConfigFile
    {
        get
        {
            return Path.Join(WorkDir, $"Pulumi.yaml");
        }
    }

    string _stackName = "azurecloudspace-handler";
    string _gitUrl = "";
    string _projectPath = "";
    List<Plugin>? _plugins = new List<Plugin>();
    ILogger _logger;

    /// <summary>
    /// Clones the provided Git URL
    /// </summary>
    /// <param name="stackName"></param>
    /// <param name="gitUrl"></param>
    /// <param name="projectPath"></param>
    public RemoteProgram(string stackName, string gitUrl, string projectPath="", List<Plugin>? plugins = null)
    {
        _logger = new LoggerConfiguration()
                            .MinimumLevel.Debug()
                            .WriteTo.Console()
                            .CreateLogger();
        _plugins = plugins;
        _stackName = stackName;
        _gitUrl = gitUrl;
        _projectPath = projectPath;
        (Stack, WorkDir)  = SetupLocalPulumiProgram().Result;
        ProjectConfig = GetProjectConfig();
    }

    public record Plugin(string Name, string Version);

    private async Task<(WorkspaceStack, string)> SetupLocalPulumiProgram()
    {

        // Get repo name from http url
        var parts = _gitUrl.Split('/');
        string repoName = parts[parts.Length - 1].Replace(".git", "");

        // create temporary directory
        var tmpDir = Directory.CreateDirectory(GetTemporaryDirectory());

        // Generate fully qualified clone destination
        
        var destDir = Path.Combine(tmpDir.FullName, repoName);
        _logger.Information($"Using dest dir {destDir}");
        CloneRepo(_gitUrl, destDir);

        // Setup pulumi program in the project path
        var workDir = Path.Combine(destDir, _projectPath);
        var stackArgs = new LocalProgramArgs(_stackName, workDir);

        // Create Pulumi Stack: pulumi new
        var stack = await LocalWorkspace.CreateOrSelectStackAsync(stackArgs);

        // Install Plugins if any
        _plugins?.ForEach(x => {
            _logger.Information($"Installing Plugin {x.Name}:{x.Version}");
            stack.Workspace.InstallPluginAsync(x.Name, x.Version, PluginKind.Resource).Wait();
        });

        return (stack,  workDir);
    }
    public async Task<UpResult> Up()
    {
        // Refresh stack: pulumi refresh
        await Stack.RefreshAsync(new RefreshOptions { OnStandardOutput = _logger.Information });

        // Run a pulumi Up
        var result = await Stack.UpAsync(new UpOptions { OnStandardOutput = _logger.Information });

        // Output results
        if (result.Summary.ResourceChanges != null)
        {
            _logger.Information("update summary:");
            foreach (var change in result.Summary.ResourceChanges)
                _logger.Information($"    {change.Key}: {change.Value}");
        }

        return result;
    }

    public async Task<UpResult> PulumiUp()
    {
        return await Up();
    }
    public async Task<UpdateResult> Destroy()
    {

        // Refresh stack: pulumi refresh
        await Stack.RefreshAsync(new RefreshOptions { OnStandardOutput = _logger.Information });

        // Run a pulumi Up
        var result = await Stack.DestroyAsync(new DestroyOptions { OnStandardOutput = _logger.Information });

        // Output results
        if (result.Summary.ResourceChanges != null)
        {
            _logger.Information("Update summary:");
            foreach (var change in result.Summary.ResourceChanges)
                _logger.Information($"    {change.Key}: {change.Value}");
        }

        return result;
    }

    private string GetTemporaryDirectory()
    {
        string tempDirectory = Path.Combine(Path.GetTempPath(), "pulumi-runner", Path.GetRandomFileName());
        Directory.CreateDirectory(tempDirectory);
        return tempDirectory;
    }

    private void CloneRepo(string gitUrl, string destDir)
    {
        // Clone fails if destination already exists
        if (Directory.Exists(destDir))
        {
            _logger.Information($"Directory {destDir} exists and is not an empty directory, please fix and try again.");
            Environment.Exit(1);
        }

        // Create a delegate to handle progress events
        var gitProgress = new ProgressHandler((serverProgressOutput) =>
        {
            // Print output to console
            Console.Write(serverProgressOutput);

            // Move cursor to beginning of line
            (int left, int top) = Console.GetCursorPosition();
            Console.SetCursorPosition(0, top);

            return true;
        });

        // Clone repo using progress handler
        var x = Repository.Clone(gitUrl, destDir, new CloneOptions { OnProgress = gitProgress });
    }

    public void InjectArkData(string arkdata)
    {
        if (arkdata != "")
        {
            _logger.Information("Injecting config...");
            // Indent the data by 4 spaces. The configfile looks like this. Two spaces for top level object 'config'
            // and another two space so the config data is nested under the "<typename>:arkdata" object
            //
            //   config:
            //      azure-native:location: WestUS2
            //      <azurecloudspace>:arkdata:
            //        name: default
            //        hub:
            var input = arkdata;
            string output = string.Join("\n", input.Split('\n').Select(line => "    " + line));


            //Stack.SetConfigAsync("dummydata", new ConfigValue("arkdummydata")).Wait();

            // Prefix data with stackname as per pulumi
            var prefix = $"  {ProjectConfig.name}:arkdata:\n";
            var config = prefix + output;

            // Inject the config for this stack into the config file
            using StreamWriter writer = File.AppendText(ConfigFile);
            writer.WriteLine(config);
        }
    }

    private ProjectConfig GetProjectConfig()
    {
        var projectConfig = File.ReadAllText(ProjectConfigFile);
        var deserializer = new DeserializerBuilder().Build();
        var x = deserializer.Deserialize<ProjectConfig>(projectConfig);
        return x;
    }
}