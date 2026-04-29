# Windows Automation Tools

Collection of modern Windows applications for automation tasks.

**[English](README.md)** | [Русский](README.ru.md)

## Projects

### 1. Auto Typer
A modern Windows application for automated text input with a sleek dark theme interface.

[📖 Documentation](README.md#auto-typer-1) | [📁 Source Code](AutoTyper.csproj)

### 2. Review Generator
AI-powered review generation tool with OCR support for extracting text from images.

[📖 Documentation](ReviewGenerator/README.md) | [📁 Source Code](ReviewGenerator/)

---

# Auto Typer

## Features

- 🎯 **Target Window Selection** - Click-to-select any window for typing
- ⌨️ **Automated Typing** - Simulates natural keyboard input using SendInput API
- ⚡ **Adjustable Speed** - Configure delay between characters (0-5000ms)
- 🌙 **Dark Theme** - Modern, clean interface with dark mode
- 🔥 **Hotkey Support** - Press F9 to quickly start/stop typing
- 📝 **Multi-line Support** - Handles text with line breaks (Shift+Enter)
- 🌍 **Unicode Support** - Works with Russian, English, and other languages

## Requirements

- Windows 7/8/10/11
- .NET 6.0 Runtime or later

## Installation

1. Download the latest release from the [Releases](../../releases) page
2. Extract `AutoTyper.exe` from the archive
3. Run `AutoTyper.exe`

## Building from Source

### Prerequisites
- .NET 6.0 SDK or later
- Visual Studio 2022 or JetBrains Rider (optional)

### Build Instructions

```bash
# Clone the repository
git clone https://github.com/YOUR_USERNAME/auto-typer.git
cd auto-typer

# Build the project
dotnet build -c Release

# Or publish as single executable
dotnet publish -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -o publish
```

The executable will be in `publish\AutoTyper.exe`

## Usage

1. **Enter Text**: Type or paste the text you want to automate in the text input field
2. **Select Target Window**: Click the "Select" button and then click on the window where you want the text to appear
3. **Set Typing Speed**: Adjust the delay between characters (default: 50ms)
4. **Start Typing**: Click "Start Typing" or press F9
5. **Stop**: Click "Stop" or press F9 again to cancel

### Important Notes

- ⚠️ **Do not click on the Auto Typer window while typing is in progress** - this will redirect the input to the Auto Typer window itself
- The window will stay on top during typing to show progress
- The target window must remain open during typing

## Hotkeys

- `F9` - Start/Stop typing

## Technical Details

- Built with C# and Windows Forms
- Uses Win32 `SendInput` API for keyboard simulation
- Supports Unicode characters via `KEYEVENTF_UNICODE` flag
- Window selection via `WindowFromPoint` API

## Project Structure

```
auto-typer/
├── MainForm.cs              # Main application window
├── WindowSelectorForm.cs    # Window selection dialog
├── KeyboardSimulator.cs     # SendInput wrapper for typing
├── NativeMethods.cs         # Win32 API declarations
├── Program.cs               # Application entry point
├── AutoTyper.csproj         # Project file
└── app.manifest             # Application manifest
```

## License

MIT License - feel free to use and modify

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## Disclaimer

This tool is for legitimate automation purposes only. Use responsibly and in accordance with the terms of service of any applications you use it with.
