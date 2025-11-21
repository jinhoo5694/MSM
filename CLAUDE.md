# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

MSM (재고 관리 시스템) is a Korean inventory management desktop application built with Avalonia UI and .NET 9.0. It handles barcode scanning, product inventory tracking, stock level alerts, and Excel-based reporting.

## Build & Run Commands

```bash
# Build
dotnet build

# Run
dotnet run

# Release build
dotnet build -c Release

# Publish self-contained executable
dotnet publish -c Release -o ./publish --self-contained -r win-x64
```

## Architecture

### MVVM Pattern
- **Models/**: Data structures (`Product.cs`)
- **ViewModels/**: Business logic and commands
- **Views/**: XAML UI components with minimal code-behind
- **Services/**: Data persistence via `IStockService` interface

### Key Data Flow
UI Events → Commands (AsyncRelayCommand) → ViewModels → Services → Excel files (.xlsx)

### Core Components

| File | Purpose |
|------|---------|
| `MainWindowViewModel.cs` | Product listing, search, dialog orchestration |
| `StockService.cs` | Excel persistence (EPPlus), change logging to JSON |
| `Product.cs` | Data model with Barcode, Name, Quantity, AlertQuantity, SafeQuantity |
| `ProductViewModel.cs` | Product card display with alert/warning/safe status logic |

### Command Pattern
Custom implementations in `Commands/RelayCommand.cs`:
- `RelayCommand`, `RelayCommand<T>` (synchronous)
- `AsyncRelayCommand`, `AsyncRelayCommand<T>` (asynchronous)

### Dialog Pattern
1. ViewModel defines event for showing dialog
2. MainWindow.axaml.cs subscribes and shows dialog window
3. Dialog returns data via `Close(T)` method

## Key Dependencies

- **Avalonia 11.3.x**: Cross-platform UI framework
- **Semi.Avalonia 11.2.x**: Modern Material-inspired theme with 40+ components
- **EPPlus 8.1.1**: Excel file manipulation (non-commercial license)

## UI Framework (Semi.Avalonia)

The project uses Semi.Avalonia for modern styling. Key features:

### Theme Resources
Use dynamic resources for consistent styling:
- Colors: `SemiGrey0Color` through `SemiGrey9Color`, `SemiRed6Color`, `SemiOrange6Color`
- Button classes: `Primary`, `Danger`

### Styling Patterns
```xml
<!-- Card-like containers -->
<Border Background="{DynamicResource SemiGrey0Color}"
        CornerRadius="8"
        Padding="20"
        BoxShadow="0 2 8 0 #10000000">

<!-- Primary button -->
<Button Content="저장" Classes="Primary"/>

<!-- Danger button -->
<Button Content="삭제" Classes="Danger"/>
```

### Adding Semi.Avalonia Styles to App.axaml
```xml
<Application.Styles>
    <StyleInclude Source="avares://Semi.Avalonia/Themes/Index.axaml" />
    <StyleInclude Source="avares://Semi.Avalonia.DataGrid/Index.axaml" />
</Application.Styles>
```

## Data Storage

- **stock.xlsx**: Product data (columns: Barcode, Name, Quantity, DefaultReductionAmount, ImagePath, AlertQuantity, SafeQuantity)
- **stock_logs.json**: Change history (JSON lines format with timestamp, before/after quantities, reason)

## Stock Level System

Products have three quantity thresholds that control border colors in `ProductViewModel`:
- `AlertQuantity`: Red border when `Quantity ≤ AlertQuantity`
- `SafeQuantity`: Gray border when `Quantity ≥ SafeQuantity`
- Warning zone: Orange border when between alert and safe

## Adding New Features

### New Product Property
1. Add to `Models/Product.cs`
2. Add Excel column header in `StockService.cs`
3. Add to `ProductViewModel.cs` for UI display
4. Add XAML bindings in views

### New Command
1. Create in ViewModel as `AsyncRelayCommand`
2. Bind in XAML: `{Binding CommandName}`

## Important Notes

- **Full-screen mode**: MainWindow is configured as full-screen, non-resizable
- **Auto-export**: 2 AM daily export triggered by 1-minute timer
- **EPPlus license**: Placeholder set in Program.cs - update `ExcelPackage.License.SetNonCommercialPersonal("Your Name")`
- **Korean UI**: All user-facing text is in Korean
- **No automated tests**: Manual testing required

## Naming Conventions

- Private fields: `_camelCase`
- Public properties: `PascalCase`
- Commands: suffixed with `Command`
- ViewModels: suffixed with `ViewModel`
- Services: interface prefixed with `I`, implementation suffixed with `Service`
