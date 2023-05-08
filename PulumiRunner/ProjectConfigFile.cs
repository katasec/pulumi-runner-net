using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Serialization;

namespace Katasec;

public class ProjectConfig
{
    [YamlMember(Alias = "name")]
    public required string name { get; set; }

    [YamlMember(Alias = "runtime")]
    public required string runtime { get; set; }

    [YamlMember(Alias = "description")]
    public required string description { get; set; }
}
