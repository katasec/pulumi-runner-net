using Katasec.PulumiRunner;

var p = new RemoteProgramArgs("azurecloudspace", "https://github.com/katasec/library.git", "azurecloudspace-handler");
await p.PulumiUp();

