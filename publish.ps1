# get git tag
$git_tag = $(git describe --tags --abbrev=0)


# dotnet pack version and specify project
$mycmd = "dotnet pack -c Release -p:PackageVersion=$git_tag -p:Project=/PulumiRunner/RemoteProgramArgs.csproj"
$mycmd
Invoke-Expression $mycmd


# Upload to nuget
$nugetPath = "./PulumiRunner/bin/Release/Katasec.PulumiRunner.$git_tag.nupkg"
$mycmd = "dotnet nuget push $nugetPath -k $env:NUGET_API_KEY -s https://api.nuget.org/v3/index.json"
$mycmd
Invoke-Expression $mycmd

