using Katasec.PulumiRunner;
using Pulumi;
using YamlDotNet.Serialization;


string readYamlToAdd()
{
    var path = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
    path = Path.Join(path, "tmp", "test.yaml");
    var txt = File.ReadAllText(path);
    return txt;
}

var arkdata = readYamlToAdd();

var p = new RemoteProgram(
    stackName: "dev", 
    gitUrl:"https://github.com/katasec/library.git", 
    projectPath: "azurecloudspace-handler",
    plugins: new List<RemoteProgram.Plugin>
    {
        new RemoteProgram.Plugin("azure-native", "v1.39.0")
    }
);

p.InjectArkData(arkdata);


