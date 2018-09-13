Param
(
    [String]$CakeVersion = "0.30.0",
    [String]$ToolPath    = [io.path]::combine($PSScriptRoot, "tools"),
    [String]$ToolExe     = [io.path]::combine($ToolPath, "dotnet-cake")
)

dotnet tool install --tool-path "$ToolPath" Cake.Tool --version $CakeVersion
& $ToolExe '--verbosity=verbose'
