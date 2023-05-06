﻿using LibGit2Sharp;
using LibGit2Sharp.Handlers;
using Pulumi;
using Pulumi.Automation;
using System;
using System.Diagnostics;

namespace Katasec.PulumiRunner;

public class RemoteProgramArgs
{

    public string WorkDir {get;}
    public WorkspaceStack Stack { get; }

    string _stackName = "azurecloudspace-handler";
    string _gitUrl = "";
    string _projectPath = "";

    public RemoteProgramArgs(string stackName, string gitUrl, string projectPath="")
    {
        _stackName = stackName;
        _gitUrl = gitUrl;
        _projectPath = projectPath;
        (Stack, WorkDir)  = SetupLocalPulumiProgram().Result;
    }


    private async Task<(WorkspaceStack, string)> SetupLocalPulumiProgram()
    {

        // Get repo name from http url
        var parts = _gitUrl.Split('/');
        string repoName = parts[parts.Length - 1].Replace(".git", "");

        // create temporary directory
        var tmpDir = Directory.CreateDirectory(GetTemporaryDirectory());

        // Generate fully qualified clone destination
        var destDir = Path.Combine(tmpDir.FullName, repoName);
        Console.WriteLine($"Using dest dir {destDir}");
        CloneRepo(_gitUrl, destDir);

        // Setup pulumi program in the project path
        var workDir = Path.Combine(destDir, _projectPath);
        var stackArgs = new LocalProgramArgs(_stackName, workDir);

        // Create Pulumi Stack: pulumi new
        var stack = await LocalWorkspace.CreateOrSelectStackAsync(stackArgs);


        return (stack,  workDir);
    }
    public async Task<UpResult> Up()
    {
        // Refresh stack: pulumi refresh
        await Stack.RefreshAsync(new RefreshOptions { OnStandardOutput = Console.WriteLine });

        // Run a pulumi Up
        var result = await Stack.UpAsync(new UpOptions { OnStandardOutput = Console.WriteLine });

        // Output results
        if (result.Summary.ResourceChanges != null)
        {
            Console.WriteLine("update summary:");
            foreach (var change in result.Summary.ResourceChanges)
                Console.WriteLine($"    {change.Key}: {change.Value}");
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
        await Stack.RefreshAsync(new RefreshOptions { OnStandardOutput = Console.WriteLine });

        // Run a pulumi Up
        var result = await Stack.DestroyAsync(new DestroyOptions { OnStandardOutput = Console.WriteLine });

        // Output results
        if (result.Summary.ResourceChanges != null)
        {
            Console.WriteLine("Update summary:");
            foreach (var change in result.Summary.ResourceChanges)
                Console.WriteLine($"    {change.Key}: {change.Value}");
        }

        return result;
    }

    private string GetTemporaryDirectory()
    {
        string tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDirectory);
        return tempDirectory;
    }

    private void CloneRepo(string gitUrl, string destDir)
    {
        // Clone fails if destination already exists
        if (Directory.Exists(destDir))
        {
            Console.WriteLine($"Directory {destDir} exists and is not an empty directory, please fix and try again.");
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
}