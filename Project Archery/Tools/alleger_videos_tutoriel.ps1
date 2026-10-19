# Allège les vidéos du tutoriel (Assets/_Project/Resources/Tutoriel) : les ré-encode en H.264, 1280 x 720, 30 images/s,
# 5 Mbit/s, sans le son, avec l'encodeur de Windows (Media Foundation). Unity Recorder les écrit environ 4 fois plus
# lourdes (chaque image est une image complète). Les .meta sont gardés : rien à refaire dans Unity.
#
# Usage, depuis le dossier « Project Archery » :
#   powershell -ExecutionPolicy Bypass -File Tools\alleger_videos_tutoriel.ps1               (toutes les vidéos)
#   powershell -ExecutionPolicy Bypass -File Tools\alleger_videos_tutoriel.ps1 -Name Tir_1   (une seule)
#
# Une vidéo déjà légère (moins de 8 Mbit/s) est laissée telle quelle. L'original est copié dans le dossier
# « Tutoriel_originaux », à côté du dossier du dépôt (hors de Git).
param(
    [string]$Name = '',
    [int]$Bitrate = 5000000,
    [int]$FrameRate = 30
)

$ErrorActionPreference = 'Stop'
$folder = Join-Path $PSScriptRoot '..\Assets\_Project\Resources\Tutoriel' | Resolve-Path
$backup = Join-Path $PSScriptRoot '..\..\..\Tutoriel_originaux'
$work = Join-Path ([System.IO.Path]::GetTempPath()) 'thunk_videos_tutoriel'
New-Item -ItemType Directory -Force $backup, $work | Out-Null

Add-Type -AssemblyName System.Runtime.WindowsRuntime
$extensions = [System.WindowsRuntimeSystemExtensions].GetMethods()
$asTaskOperation = $extensions | Where-Object { $_.Name -eq 'AsTask' -and $_.GetParameters().Count -eq 1 -and $_.GetParameters()[0].ParameterType.Name -eq 'IAsyncOperation`1' } | Select-Object -First 1
$asTaskProgress = $extensions | Where-Object { $_.Name -eq 'AsTask' -and $_.GetParameters().Count -eq 1 -and $_.GetParameters()[0].ParameterType.Name -eq 'IAsyncActionWithProgress`1' } | Select-Object -First 1

function Await($operation, [Type]$resultType) {
    $task = $asTaskOperation.MakeGenericMethod($resultType).Invoke($null, @($operation))
    $task.Wait(-1) | Out-Null
    return $task.Result
}

function AwaitProgress($action) {
    $task = $asTaskProgress.MakeGenericMethod([double]).Invoke($null, @($action))
    $task.Wait(-1) | Out-Null
}

[Windows.Storage.StorageFile, Windows.Storage, ContentType = WindowsRuntime] | Out-Null
[Windows.Storage.StorageFolder, Windows.Storage, ContentType = WindowsRuntime] | Out-Null
[Windows.Storage.FileProperties.VideoProperties, Windows.Storage, ContentType = WindowsRuntime] | Out-Null
[Windows.Media.Transcoding.MediaTranscoder, Windows.Media.Transcoding, ContentType = WindowsRuntime] | Out-Null
[Windows.Media.MediaProperties.MediaEncodingProfile, Windows.Media.MediaProperties, ContentType = WindowsRuntime] | Out-Null

$workFolder = Await ([Windows.Storage.StorageFolder]::GetFolderFromPathAsync((Resolve-Path $work).Path)) ([Windows.Storage.StorageFolder])
$filter = if ($Name) { "$Name.mp4" } else { '*.mp4' }

foreach ($file in Get-ChildItem $folder -Filter $filter) {
    $source = Await ([Windows.Storage.StorageFile]::GetFileFromPathAsync($file.FullName)) ([Windows.Storage.StorageFile])
    $properties = Await ($source.Properties.GetVideoPropertiesAsync()) ([Windows.Storage.FileProperties.VideoProperties])
    $seconds = [Math]::Max(0.1, $properties.Duration.TotalSeconds)
    $megabits = $file.Length * 8 / $seconds / 1e6
    if ($megabits -lt 8) {
        Write-Output ("{0} : déjà légère ({1:N1} Mbit/s), gardée." -f $file.Name, $megabits)
        continue
    }

    $target = Await ($workFolder.CreateFileAsync($file.Name, [Windows.Storage.CreationCollisionOption]::ReplaceExisting)) ([Windows.Storage.StorageFile])
    $profile = [Windows.Media.MediaProperties.MediaEncodingProfile]::CreateMp4([Windows.Media.MediaProperties.VideoEncodingQuality]::HD720p)
    $profile.Audio = $null
    $profile.Video.Width = 1280
    $profile.Video.Height = 720
    $profile.Video.Bitrate = $Bitrate
    $profile.Video.FrameRate.Numerator = $FrameRate
    $profile.Video.FrameRate.Denominator = 1

    $transcoder = New-Object Windows.Media.Transcoding.MediaTranscoder
    $transcoder.HardwareAccelerationEnabled = $true
    $prepared = Await ($transcoder.PrepareFileTranscodeAsync($source, $target, $profile)) ([Windows.Media.Transcoding.PrepareTranscodeResult])
    if (-not $prepared.CanTranscode) {
        Write-Output ("{0} : impossible à ré-encoder ({1}), gardée." -f $file.Name, $prepared.FailureReason)
        continue
    }

    AwaitProgress ($prepared.TranscodeAsync())
    $light = Get-Item (Join-Path $work $file.Name)
    Copy-Item $file.FullName (Join-Path $backup $file.Name) -Force
    Copy-Item $light.FullName $file.FullName -Force
    Remove-Item $light.FullName
    Write-Output ("{0} : {1:N1} Mo -> {2:N1} Mo" -f $file.Name, ($file.Length / 1MB), ($light.Length / 1MB))
}
