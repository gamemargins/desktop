# Builds GameMargins Uploader with the C# compiler every Windows 10 and 11 ships in the .NET Framework (csc.exe of
# .NET 4.x): nothing to install, and the program runs on any Windows with .NET Framework 4.7.2 or later, which both
# have built in. One small exe, no installer, no runtime to download.
#
#   powershell -NoProfile -ExecutionPolicy Bypass -File build.ps1
#
# Output: dist\GameMarginsUploader.exe, and the same file as dist\GameMarginsUploader-<version>.exe, the name a release
# publishes it under. The version is Program.Version in src\Program.cs, the one place it is written
# (src\AssemblyInfo.cs stamps the same constant into the exe's file data, which the build checks). The build is not
# signed, so Windows SmartScreen warns on the first start (see README.md).
#
# That compiler speaks C# 5 (async/await yes; string interpolation, ?. , nameof and digit separators no), which the
# sources keep to. The icon, GameMargins.ico, is stamped on the exe and embedded as a resource for the tray. The
# notifications are Windows toasts (src\Toasts.cs), so the build also references the Windows Runtime's metadata that
# every Windows 10 and 11 ships in System32\WinMetadata (Windows.UI, Windows.Data, Windows.Foundation) and the
# framework's WindowsRuntime bridges.

$ErrorActionPreference = "Stop"
$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$framework = Join-Path $env:WINDIR "Microsoft.NET\Framework64\v4.0.30319"
if (-not (Test-Path (Join-Path $framework "csc.exe"))) { $framework = Join-Path $env:WINDIR "Microsoft.NET\Framework\v4.0.30319" }
$csc = Join-Path $framework "csc.exe"
if (-not (Test-Path $csc)) { throw "csc.exe not found under $env:WINDIR\Microsoft.NET: the .NET Framework 4.x is missing" }

$found = [regex]::Match([IO.File]::ReadAllText((Join-Path $here "src\Program.cs")), 'public const string Version = "(\d+\.\d+\.\d+)";')
if (-not $found.Success) { throw 'src\Program.cs: no public const string Version = "x.y.z"; found' }
$version = $found.Groups[1].Value

$dist = Join-Path $here "dist"
New-Item -ItemType Directory -Force $dist | Out-Null
$out = Join-Path $dist "GameMarginsUploader.exe"
$sources = Get-ChildItem (Join-Path $here "src") -Filter *.cs | ForEach-Object { $_.FullName }
$icon = Join-Path $here "GameMargins.ico"
if (-not (Test-Path $icon)) { throw "GameMargins.ico is missing" }
$manifest = Join-Path $here "src\app.manifest"

$refs = @("System.dll", "System.Core.dll", "System.Drawing.dll", "System.Windows.Forms.dll", "System.Net.Http.dll", "System.Security.dll", "System.Web.Extensions.dll", "System.IO.Compression.dll", "System.Runtime.dll", "System.Runtime.WindowsRuntime.dll", "System.Runtime.InteropServices.WindowsRuntime.dll") | ForEach-Object { "/reference:" + (Join-Path $framework $_) }
# The Windows Runtime's metadata for the toasts (Toasts.cs): Windows.UI.Notifications, Windows.Data.Xml.Dom and their base types.
$winmd = Join-Path $env:WINDIR "System32\WinMetadata"
foreach ($m in @("Windows.UI.winmd", "Windows.Data.winmd", "Windows.Foundation.winmd")) {
  $file = Join-Path $winmd $m
  if (-not (Test-Path $file)) { throw "$file is missing: the Windows Runtime metadata the notifications need (Windows 10 or 11)" }
  $refs += "/reference:$file"
}

& $csc /nologo /target:winexe /platform:anycpu /optimize+ /warn:4 /nowarn:1591 `
  "/out:$out" "/win32icon:$icon" "/win32manifest:$manifest" "/resource:$icon,GameMargins.Uploader.GameMargins.ico" `
  $refs $sources
if ($LASTEXITCODE -ne 0) { throw "csc failed with exit code $LASTEXITCODE" }
# The file data Windows shows (read from the file, never by running it): the version Program.cs names.
$info = (Get-Item $out).VersionInfo
if ($info.FileVersion -ne "$version.0" -or $info.ProductVersion -ne $version) {
  throw "the exe's file data says $($info.FileVersion) / $($info.ProductVersion), not $version.0 / $version (src\AssemblyInfo.cs)"
}
$size = [math]::Round((Get-Item $out).Length / 1KB)
Write-Host "Built $out ($size KB, version $version)"

# The release's file, under the version's name; older versioned builds in dist\ go.
$releaseName = "GameMarginsUploader-$version.exe"
Get-ChildItem $dist -Filter "GameMarginsUploader-*.exe" | Where-Object { $_.Name -ne $releaseName } | Remove-Item
$release = Join-Path $dist $releaseName
Copy-Item $out $release -Force
Write-Host "Release file $release"
Write-Host "SHA-256 $((Get-FileHash $release -Algorithm SHA256).Hash.ToLowerInvariant())"
