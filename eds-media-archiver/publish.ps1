$publishDir = $PSScriptRoot + '/publish'
dotnet publish -c Release -o $publishDir
