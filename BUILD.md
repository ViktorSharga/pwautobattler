# GameAutomation Build Guide

## Building the Application

### Prerequisites
- .NET 9.0 SDK or later
- Windows (for runtime targeting)

### Local Build

#### Debug Build
```bash
dotnet build autobattler.sln --configuration Debug
```

#### Release Build
```bash
dotnet build autobattler.sln --configuration Release
```

#### Self-Contained Executable (Recommended)
```bash
dotnet publish GameAutomation.csproj \
  --configuration Release \
  --runtime win-x64 \
  --self-contained true \
  --output ./publish/win-x64 \
  -p:PublishSingleFile=true \
  -p:IncludeNativeLibrariesForSelfExtract=true \
  -p:EnableCompressionInSingleFile=true \
  -p:DebugType=None \
  -p:DebugSymbols=false
```

This creates a single `GameAutomation.exe` file (~50MB) that includes all dependencies.

#### Framework-Dependent Build (Smaller size, requires .NET runtime)
```bash
dotnet publish GameAutomation.csproj \
  --configuration Release \
  --runtime win-x64 \
  --self-contained false \
  --output ./publish/win-x64-framework
```

### GitHub Actions CI/CD

The project includes a GitHub Actions workflow (`.github/workflows/build.yml`) that:

1. **Builds both Debug and Release configurations**
2. **Creates two types of artifacts:**
   - **Self-contained**: Single executable with all dependencies (~50MB)
   - **Framework-dependent**: Smaller size, requires .NET 9.0 runtime on target machine
3. **Validates executables** and reports file sizes
4. **Uploads artifacts** with 90-day retention
5. **Attaches releases** automatically when GitHub releases are created

### Artifact Download

After each commit to `master`, you can download the latest builds from:
- GitHub Actions → Latest workflow run → Artifacts section
- Two zip files will be available:
  - `GameAutomation-SelfContained-win-x64.zip` (Recommended)
  - `GameAutomation-FrameworkDependent-win-x64.zip`

### Build Output Structure

```
publish/
├── win-x64/                    # Self-contained build
│   └── GameAutomation.exe     # Single executable (~50MB)
└── win-x64-framework/         # Framework-dependent build
    ├── GameAutomation.exe     # Main executable
    ├── GameAutomation.dll     # Application DLL
    ├── GameAutomation.deps.json
    └── GameAutomation.runtimeconfig.json
```

### Troubleshooting

#### Build Issues
- Ensure .NET 9.0 SDK is installed: `dotnet --version`
- Clean build artifacts: `dotnet clean`
- Restore packages: `dotnet restore`

#### Runtime Issues
- **Self-contained build**: Should run on any Windows x64 machine
- **Framework-dependent build**: Requires .NET 9.0 Desktop Runtime on target machine

### Project Configuration

The project is configured with:
- **Target Framework**: .NET 9.0 Windows
- **Platform**: x64
- **Output Type**: Windows Executable (WinExe)
- **UI Framework**: Windows Forms
- **Single File Publishing**: Enabled
- **Compression**: Enabled for single file
- **Native Libraries**: Included in self-extract

### File Sizes

| Build Type | Approximate Size | Dependencies |
|------------|------------------|--------------|
| Self-contained | ~50MB | None (all included) |
| Framework-dependent | ~500KB | Requires .NET 9.0 Runtime |
| Debug build | ~200KB | Requires .NET 9.0 Runtime |