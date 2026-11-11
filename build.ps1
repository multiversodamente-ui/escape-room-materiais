$ErrorActionPreference = 'Stop'
$projectFolder = Join-Path $PSScriptRoot 'src'
$destinationFolder = Join-Path $PSScriptRoot 'dist'
New-Item -ItemType Directory -Path $destinationFolder -Force | Out-Null
$frameworkFolder = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
if (-not (Test-Path -LiteralPath (Join-Path $frameworkFolder 'csc.exe'))) {
    $frameworkFolder = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319'
}
$compiler = Join-Path $frameworkFolder 'csc.exe'
if (-not (Test-Path -LiteralPath $compiler)) { throw 'O compilador do .NET Framework não foi encontrado neste Windows.' }
$buildArgs = @('/nologo','/target:winexe','/optimize+','/platform:anycpu','/utf8output',('/out:' + (Join-Path $destinationFolder 'Cronometro Tres Salas.exe')),('/win32icon:' + (Join-Path $projectFolder 'assets\app.ico')),('/win32manifest:' + (Join-Path $projectFolder 'app.manifest')))
foreach ($assembly in @('System.dll','System.Core.dll','System.Xml.dll','System.Xaml.dll','System.Drawing.dll','System.Windows.Forms.dll')) { $buildArgs += '/reference:' + (Join-Path $frameworkFolder $assembly) }
foreach ($assembly in @('PresentationFramework.dll','PresentationCore.dll','WindowsBase.dll')) { $buildArgs += '/reference:' + (Join-Path $frameworkFolder ('WPF\' + $assembly)) }
foreach ($name in @('Dashboard.xaml','Projection.xaml','CREDITOS.txt')) { $buildArgs += '/resource:' + (Join-Path $projectFolder $name) + ',' + $name }
foreach ($name in @('logo.png','app.ico','explosao.wav','estrondo.wav','colapso.wav','alarme.wav','sirene.wav','impacto.wav','queda.wav','energia.wav','faiscas.wav')) { $buildArgs += '/resource:' + (Join-Path $projectFolder ('assets\' + $name)) + ',' + $name }
foreach ($name in @('TimerCore.cs','Audio.cs','SoundPlan.cs','MultiAudio.cs','ThemeArt.cs','Projection.cs','Program.cs','ShowSettings.cs','Tests.cs')) { $buildArgs += Join-Path $projectFolder $name }
& $compiler @buildArgs
if ($LASTEXITCODE -ne 0) { throw 'Não foi possível compilar o aplicativo.' }
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'CREDITOS.txt') -Destination $destinationFolder -Force
Write-Output (Join-Path $destinationFolder 'Cronometro Tres Salas.exe')
