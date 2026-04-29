# Tesseract Language Data Download Script
# This script downloads Russian and English language files for Tesseract OCR

$tessDataPath = ".\tessdata"
$baseUrl = "https://github.com/tesseract-ocr/tessdata/raw/main"

# Create tessdata directory if it doesn't exist
if (-not (Test-Path $tessDataPath)) {
    New-Item -ItemType Directory -Path $tessDataPath | Out-Null
    Write-Host "Created tessdata directory"
}

# Download Russian language file
$rusFile = "$tessDataPath\rus.traineddata"
if (-not (Test-Path $rusFile)) {
    Write-Host "Downloading Russian language file..."
    Invoke-WebRequest -Uri "$baseUrl/rus.traineddata" -OutFile $rusFile
    Write-Host "Russian language file downloaded"
} else {
    Write-Host "Russian language file already exists"
}

# Download English language file
$engFile = "$tessDataPath\eng.traineddata"
if (-not (Test-Path $engFile)) {
    Write-Host "Downloading English language file..."
    Invoke-WebRequest -Uri "$baseUrl/eng.traineddata" -OutFile $engFile
    Write-Host "English language file downloaded"
} else {
    Write-Host "English language file already exists"
}

Write-Host "`nTesseract language files are ready!"
Write-Host "Location: $tessDataPath"
