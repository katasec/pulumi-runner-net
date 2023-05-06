using Katasec.PulumiRunner;

var p = new RemoteProgramArgs("azurecloudspace", "https://github.com/katasec/library.git", "azurecloudspace-handler");

Console.WriteLine("WorkDir:" + p.WorkDir);
Console.WriteLine("Stack:" + p.Stack.Name);
