$source = "d:\Ark4Studio\Extentions\Autocat\Autocat.bundle"
$destination = "$env:APPDATA\Autodesk\ApplicationPlugins\Autocat.bundle"

Write-Host "Waiting for AutoCAD to close if running..."
while (Get-Process acad -ErrorAction SilentlyContinue) {
    Write-Host "AutoCAD is running. Please close AutoCAD to complete plugin update..."
    Start-Sleep -Seconds 2
}

Write-Host "Updating Autocat.bundle..."
Copy-Item -Recurse -Force "$source\*" $destination
Write-Host "Autocat updated successfully! You can now reopen AutoCAD."
