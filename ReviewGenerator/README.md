# Review Generator

AI-powered review generation tool with OCR support for extracting text from images (receipts, business cards, etc.).

**[English](README.md)** | [Русский](README.ru.md)

## Features

- 📸 **Image Upload** - Upload up to 5 images (receipts, business cards, documents)
- 📋 **Clipboard Support** - Press Ctrl+V to paste images directly from clipboard
- 🔍 **OCR Text Extraction** - Automatically extract text from images using Windows built-in OCR
- 🤖 **AI-Powered Reviews** - Generate authentic reviews using Claude AI
- 🏪 **Service Types** - Support for restaurants, shops, medical services, hotels, and more
- 🌍 **Russian Language** - Generates reviews in Russian with proper grammar
- 🎨 **Modern Dark UI** - Clean, professional dark theme interface
- ⚙️ **Configurable** - API settings via appsettings.json

## Requirements

- Windows 10 (version 1809 or later) or Windows 11
- .NET 6.0 Runtime or later
- Local AI API endpoint (OpenCode or compatible Claude API)

## Installation

1. Download the latest release from the [Releases](../../releases) page
2. Extract the archive
3. Configure `appsettings.json` with your API settings
4. Run `ReviewGenerator.exe`

## Configuration

Edit `appsettings.json` to configure API settings:

```json
{
  "ApiSettings": {
    "ApiKey": "your-api-key-here",
    "ApiEndpoint": "http://localhost:20128/v1",
    "Model": "kr/claude-sonnet-4.5"
  }
}
```

## Building from Source

### Prerequisites
- .NET 6.0 SDK or later
- Visual Studio 2022 or JetBrains Rider (optional)

### Build Instructions

```bash
# Clone the repository
git clone https://github.com/YOUR_USERNAME/review-generator.git
cd review-generator/ReviewGenerator

# Build the project
dotnet build -c Release

# Or publish as single executable
dotnet publish -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -o publish
```

The executable will be in `publish\ReviewGenerator.exe`

## Usage

1. **Upload Images**: Click "Add Images" or press Ctrl+V to paste from clipboard
2. **Select Service Type**: Choose the type of service (restaurant, shop, medical, etc.)
3. **Add Instructions** (optional): Provide additional context or requirements
4. **Generate Review**: Click "Generate Review" button
5. **View Results**: 
   - OCR extracted text appears in the middle section
   - Generated review appears at the bottom
6. **Copy Review**: Click "Copy" to copy the review to clipboard

### Supported Image Formats

- JPG/JPEG
- PNG
- BMP
- GIF

### Service Types

- Restaurant / Cafe
- Shop / Store
- Service / Repair
- Hotel / Accommodation
- Medical / Healthcare
- Beauty / Salon
- Other

## Technical Details

- Built with C# and Windows Forms
- Uses Windows.Media.Ocr (built-in Windows OCR) for text extraction
- Supports multiple languages based on Windows language settings
- Integrates with Claude AI API for review generation
- Configuration management via Microsoft.Extensions.Configuration
- Modern dark theme UI with compact, clean layout
- Completely free and open-source with no license requirements

## Project Structure

```
ReviewGenerator/
├── MainForm.cs              # Main application window and logic
├── Program.cs               # Application entry point
├── ReviewGenerator.csproj   # Project file
└── appsettings.json         # Configuration file
```

## Dependencies

- **Windows.Media.Ocr** (built-in) - OCR text extraction
- **Anthropic.SDK** (5.10.0) - Claude AI integration
- **Microsoft.Extensions.Configuration.Json** (6.0.1) - Configuration management

All dependencies are free and open-source.

## License

MIT License - feel free to use and modify

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## Disclaimer

This tool is for legitimate purposes only. Ensure you have permission to use any images and comply with the terms of service of the AI API provider.
