# Define constants.
$PSScriptRoot = split-path -parent $MyInvocation.MyCommand.Definition;
$Script = Join-Path $PSScriptRoot "build.cake"
$ToolPath = Join-Path $PSScriptRoot "dependencies/Nancy/tools"
$NuGetPath = Join-Path $ToolPath "nuget/NuGet.exe"
$CakeVersion = "0.13.0"
$CakePath = Join-Path $ToolPath "Cake.$CakeVersion/Cake.exe"
$Target = "Default"
$Verbosity = "Verbose"
$DryRun
$Arguments = @{}

for($i=0; $i -lt $args.length; $i+=1)
{
  if ($args[$i].ToLower() -eq "-target")
  {
    $Target = $args[$i+1]
    $i+=1
  }
  ElseIf ($args[$i].ToLower() -eq "-verbosity")
  {
    $Verbosity = $args[$i+1]
    $i+=1
  }
  ElseIf ($args[$i].ToLower() -eq "-dryrun")
  {
    $DryRun = "-dryrun"
  }
  Else
  {
    $Arguments.Add($args[$i], $args[$i+1])
    $i+=1
  }
}
######################################################################################################

Function Install-Dotnet()
{
  $existingPaths = $Env:Path -Split ';' | Where-Object { (![string]::IsNullOrEmpty($_)) -and (Test-Path $_) }
  $DOTNET_EXE_IN_PATH = (Get-ChildItem -Path $existingPaths -Filter "dotnet.exe" | Select -First 1).FullName
  $GlobalDotNetFound =  (![string]::IsNullOrEmpty($DOTNET_EXE_IN_PATH)) -and (Test-Path $DOTNET_EXE_IN_PATH)
  $LocalDotNetFound = Test-Path (Join-Path $PSScriptRoot ".dotnet")

  if ($GlobalDotNetFound)
  {
    return
  }

  if((!$LocalDotNetFound) -Or ((Test-Path Env:\APPVEYOR) -eq $true))
  {
    Write-Output "Dotnet CLI was not found."

    # Prepare the dotnet CLI folder
    $env:DOTNET_INSTALL_DIR="$(Convert-Path "$PSScriptRoot")\.dotnet\win7-x64"
    if (!(Test-Path $env:DOTNET_INSTALL_DIR))
    {
      mkdir $env:DOTNET_INSTALL_DIR | Out-Null
    }

    # Download the dotnet CLI install script
    if (!(Test-Path .\dotnet\install.ps1))
    {
      Write-Output "Downloading version 1.0.0-preview2 of Dotnet CLI installer..."
      Invoke-WebRequest "https://raw.githubusercontent.com/dotnet/cli/rel/1.0.0-preview2/scripts/obtain/dotnet-install.ps1" -OutFile ".\.dotnet\dotnet-install.ps1"
    }

    # Run the dotnet CLI install
    Write-Output "Installing Dotnet CLI version 1.0.0-preview2-003131..."
    & .\.dotnet\dotnet-install.ps1 -Channel beta -Version 1.0.0-preview2-003131

    # Add the dotnet folder path to the process. This gets skipped
    # by Install-DotNetCli if it's already installed.
    Remove-PathVariable $env:DOTNET_INSTALL_DIR
    $env:PATH = "$env:DOTNET_INSTALL_DIR;$env:PATH"
  }
}

Function Remove-PathVariable([string]$VariableToRemove)
{
  $path = [Environment]::GetEnvironmentVariable("PATH", "User")
  $newItems = $path.Split(';') | Where-Object { $_.ToString() -inotlike $VariableToRemove }
  [Environment]::SetEnvironmentVariable("PATH", [System.String]::Join(';', $newItems), "User")
  $path = [Environment]::GetEnvironmentVariable("PATH", "Process")
  $newItems = $path.Split(';') | Where-Object { $_.ToString() -inotlike $VariableToRemove }
  [Environment]::SetEnvironmentVariable("PATH", [System.String]::Join(';', $newItems), "Process")
}

######################################################################################################

Write-Host "Preparing to run build script..."

# Install Dotnet CLI.
Install-Dotnet

# Make sure Cake has been installed.
if (!(Test-Path $CakePath)) {
    Write-Verbose "Installing Cake..."
    Invoke-Expression "&`"$NuGetPath`" install Cake -Version $CakeVersion -OutputDirectory `"$ToolPath`"" | Out-Null;
    if ($LASTEXITCODE -ne 0) {
        Throw "An error occured while restoring Cake from NuGet."
    }
}

# Build the argument list.
$Arguments = $Arguments.GetEnumerator() | %{"{0}=`"{1}`"" -f $_.key, $_.value };

# Start Cake.
Write-Host "Running build script..."
Write-Host "`"$CakePath`" `"$Script`" -target=`"$Target`" -verbosity=`"$Verbosity`" $DryRun $Arguments"
Invoke-Expression "& `"$CakePath`" `"$Script`" -target=`"$Target`" -verbosity=`"$Verbosity`" $DryRun $Arguments"
exit $LASTEXITCODE
