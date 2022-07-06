#Requires -RunAsAdministrator
$version = "1.0.10.0"
$release = "OneTouchMonitor_$($version)"
Invoke-WebRequest "https://github.com/ClaudiaCoord/OneTouchAudioMonitor/releases/download/$($version)/$($release).zip" -OutFile ".\$($release).zip"
Set-itemproperty -Path Registry::HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\AppModelUnlock -Name AllowAllTrustedApps -Value 1
Expand-Archive -Path "$($release).zip" -DestinationPath ".\"
cd "$($release)"
Import-Certificate -FilePath "$($release)_x86_x64_arm_arm64.cer" -CertStoreLocation "Cert:\CurrentUser\Root\"
Import-Certificate -FilePath "$($release)_x86_x64_arm_arm64.cer" -CertStoreLocation "Cert:\LocalMachine\Root\"
Add-AppxPackage -Path "$($release)_x86_x64_arm_arm64.msixbundle"

