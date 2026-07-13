set shell := ["pwsh", "-NoLogo", "-NoProfile", "-Command"]

legacy_project := "Translater.csproj"
cmdpal_solution := "cmdpal/PowerTranslatorExtension.sln"
cmdpal_project := "cmdpal/PowerTranslatorExtension/PowerTranslatorExtension.csproj"
cmdpal_tests := "cmdpal/PowerTranslatorExtension.Tests/PowerTranslatorExtension.Tests.csproj"

# List the available development commands.
default: help

# List the available development commands.
help:
    @just --list

# Show the installed .NET SDK and just versions.
info:
    dotnet --info
    just --version

# Build the legacy PowerToys Run plugin. Output: bin/output_<platform>/.
legacy-build platform="x64" configuration="Debug":
    dotnet build {{ legacy_project }} -c {{ configuration }} -p:Platform={{ platform }}

# Build legacy plugin outputs for both supported architectures.
legacy-build-all configuration="Debug":
    just legacy-build x64 {{ configuration }}
    just legacy-build ARM64 {{ configuration }}

# Package the legacy plugin as a ZIP. Output: bin/Translator_<platform>.zip.
legacy-pack platform="x64" configuration="Release":
    dotnet pack {{ legacy_project }} -c {{ configuration }} -p:Platform={{ platform }}

# Package legacy plugin ZIPs for both supported architectures.
legacy-pack-all configuration="Release":
    just legacy-pack x64 {{ configuration }}
    just legacy-pack ARM64 {{ configuration }}

# Restore the Command Palette solution with Visual Studio MSBuild.
cmdpal-restore configuration="Release":
    & { $vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'; if (-not (Test-Path $vswhere)) { throw 'Visual Studio MSBuild was not found. Install Visual Studio 2022 with the MSBuild and Windows App SDK workloads.' }; $msbuild = & $vswhere -latest -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\MSBuild.exe' | Select-Object -First 1; if (-not $msbuild) { throw 'vswhere could not locate MSBuild.' }; & $msbuild '{{ cmdpal_solution }}' /t:Restore /p:Configuration={{ configuration }}; exit $LASTEXITCODE }

# Run Command Palette unit tests (restores and builds through Visual Studio MSBuild first).
test configuration="Release":
    & { $vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'; if (-not (Test-Path $vswhere)) { throw 'Visual Studio MSBuild was not found. Install Visual Studio 2022 with the MSBuild and Windows App SDK workloads.' }; $msbuild = & $vswhere -latest -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\MSBuild.exe' | Select-Object -First 1; if (-not $msbuild) { throw 'vswhere could not locate MSBuild.' }; & $msbuild '{{ cmdpal_tests }}' /t:Restore /p:Configuration={{ configuration }} /p:Platform=x64 /p:GenerateAppxPackageOnBuild=false /p:EnableMsixTooling=false; if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }; & $msbuild '{{ cmdpal_tests }}' /t:Build /p:Configuration={{ configuration }} /p:Platform=x64 /p:GenerateAppxPackageOnBuild=false /p:EnableMsixTooling=false; if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }; dotnet test '{{ cmdpal_tests }}' -c {{ configuration }} -p:Platform=x64 --no-build --no-restore --logger 'console;verbosity=normal'; exit $LASTEXITCODE }

# Build the Command Palette extension MSIX for one architecture.
cmdpal-build platform="x64" configuration="Release":
    & { $vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'; if (-not (Test-Path $vswhere)) { throw 'Visual Studio MSBuild was not found. Install Visual Studio 2022 with the MSBuild and Windows App SDK workloads.' }; $msbuild = & $vswhere -latest -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\MSBuild.exe' | Select-Object -First 1; if (-not $msbuild) { throw 'vswhere could not locate MSBuild.' }; & $msbuild '{{ cmdpal_project }}' /restore /nologo /v:minimal /p:Configuration={{ configuration }} /p:Platform={{ platform }}; exit $LASTEXITCODE }

# Build Command Palette MSIX packages for x64 and ARM64.
cmdpal-build-all configuration="Release":
    just cmdpal-build x64 {{ configuration }}
    just cmdpal-build ARM64 {{ configuration }}

# Build, self-sign, and install a Command Palette MSIX for local E2E testing.
cmdpal-install-local platform="x64" configuration="Debug":
    pwsh -NoLogo -NoProfile -File cmdpal/install-local.ps1 -Platform {{ platform }} -Configuration {{ configuration }}

# Remove the local self-signing certificate and its machine trust entry.
cmdpal-clean-local:
    pwsh -NoLogo -NoProfile -File cmdpal/clean-local.ps1

# Build a Microsoft Store MSIXBundle (x64 + ARM64). Output: dist/.
cmdpal-store configuration="Release":
    pwsh -NoLogo -NoProfile -File cmdpal/build-store.ps1 -Configuration {{ configuration }}

# Build a Microsoft Store MSIXBundle containing only x64. Output: dist/.
cmdpal-store-x64 configuration="Release":
    pwsh -NoLogo -NoProfile -File cmdpal/build-store.ps1 -Configuration {{ configuration }} -Platforms x64

# Regenerate Command Palette image assets from the root Images/ directory.
cmdpal-assets:
    pwsh -NoLogo -NoProfile -File cmdpal/generate-assets.ps1

# Remove generated build output from both project variants.
clean:
    dotnet clean {{ legacy_project }}
    dotnet clean {{ cmdpal_solution }}
