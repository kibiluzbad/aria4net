param($installPath, $toolsPath, $package, $project)

$item = $project.ProjectItems | where-object {$_.Name -eq "aria2c.exe"}

$item.Properties.Item("CopyToOutputDirectory").Value = [int]2