# Projects and dependencies analysis

This document provides a comprehensive overview of the projects and their dependencies in the context of upgrading to .NETCoreApp,Version=v10.0.

## Table of Contents

- [Executive Summary](#executive-Summary)
  - [Highlevel Metrics](#highlevel-metrics)
  - [Projects Compatibility](#projects-compatibility)
  - [Package Compatibility](#package-compatibility)
  - [API Compatibility](#api-compatibility)
- [Aggregate NuGet packages details](#aggregate-nuget-packages-details)
- [Top API Migration Challenges](#top-api-migration-challenges)
  - [Technologies and Features](#technologies-and-features)
  - [Most Frequent API Issues](#most-frequent-api-issues)
- [Projects Relationship Graph](#projects-relationship-graph)
- [Project Details](#project-details)

  - [animation\animation\Animation.csproj](#animationanimationanimationcsproj)
  - [FDEMConsole\FDEMConsole.csproj](#fdemconsolefdemconsolecsproj)
  - [FDEMCore\FDEMCore.csproj](#fdemcorefdemcorecsproj)
  - [FDEMTests\FDEMTests.csproj](#fdemtestsfdemtestscsproj)
  - [FDEMWindows\FDEMWindows.csproj](#fdemwindowsfdemwindowscsproj)
  - [FxTMeshGenerator\FxTMeshGenerator.csproj](#fxtmeshgeneratorfxtmeshgeneratorcsproj)
  - [PlotFDEM\PlotFDEM.csproj](#plotfdemplotfdemcsproj)
  - [RandomRVEGenerator\RandomRVEGenerator.csproj](#randomrvegeneratorrandomrvegeneratorcsproj)
  - [RandomRVEGeneratorConsole\RandomRVEGeneratorConsole.csproj](#randomrvegeneratorconsolerandomrvegeneratorconsolecsproj)


## Executive Summary

### Highlevel Metrics

| Metric | Count | Status |
| :--- | :---: | :--- |
| Total Projects | 9 | 7 require upgrade |
| Total NuGet Packages | 11 | 2 need upgrade |
| Total Code Files | 115 |  |
| Total Code Files with Incidents | 34 |  |
| Total Lines of Code | 26369 |  |
| Total Number of Issues | 4356 |  |
| Estimated LOC to modify | 4345+ | at least 16.5% of codebase |

### Projects Compatibility

| Project | Target Framework | Difficulty | Package Issues | API Issues | Est. LOC Impact | Description |
| :--- | :---: | :---: | :---: | :---: | :---: | :--- |
| [animation\animation\Animation.csproj](#animationanimationanimationcsproj) | net48 | 🟡 Medium | 1 | 1034 | 1034+ | ClassicWinForms, Sdk Style = False |
| [FDEMConsole\FDEMConsole.csproj](#fdemconsolefdemconsolecsproj) | net5.0 | 🟢 Low | 0 | 0 |  | DotNetCoreApp, Sdk Style = True |
| [FDEMCore\FDEMCore.csproj](#fdemcorefdemcorecsproj) | net5.0 | 🟢 Low | 0 | 1 | 1+ | ClassLibrary, Sdk Style = True |
| [FDEMTests\FDEMTests.csproj](#fdemtestsfdemtestscsproj) | net10.0 | ✅ None | 0 | 0 |  | DotNetCoreApp, Sdk Style = True |
| [FDEMWindows\FDEMWindows.csproj](#fdemwindowsfdemwindowscsproj) | net5.0-windows | 🟢 Low | 1 | 22 | 22+ | WinForms, Sdk Style = True |
| [FxTMeshGenerator\FxTMeshGenerator.csproj](#fxtmeshgeneratorfxtmeshgeneratorcsproj) | net10.0 | ✅ None | 0 | 0 |  | DotNetCoreApp, Sdk Style = True |
| [PlotFDEM\PlotFDEM.csproj](#plotfdemplotfdemcsproj) | net5.0-windows | 🟡 Medium | 1 | 3215 | 3215+ | WinForms, Sdk Style = True |
| [RandomRVEGenerator\RandomRVEGenerator.csproj](#randomrvegeneratorrandomrvegeneratorcsproj) | net5.0-windows | 🟡 Medium | 0 | 73 | 73+ | WinForms, Sdk Style = True |
| [RandomRVEGeneratorConsole\RandomRVEGeneratorConsole.csproj](#randomrvegeneratorconsolerandomrvegeneratorconsolecsproj) | net5.0 | 🟢 Low | 0 | 0 |  | DotNetCoreApp, Sdk Style = True |

### Package Compatibility

| Status | Count | Percentage |
| :--- | :---: | :---: |
| ✅ Compatible | 9 | 81.8% |
| ⚠️ Incompatible | 1 | 9.1% |
| 🔄 Upgrade Recommended | 1 | 9.1% |
| ***Total NuGet Packages*** | ***11*** | ***100%*** |

### API Compatibility

| Category | Count | Impact |
| :--- | :---: | :--- |
| 🔴 Binary Incompatible | 3758 | High - Require code changes |
| 🟡 Source Incompatible | 587 | Medium - Needs re-compilation and potential conflicting API error fixing |
| 🔵 Behavioral change | 0 | Low - Behavioral changes that may require testing at runtime |
| ✅ Compatible | 19046 |  |
| ***Total APIs Analyzed*** | ***23391*** |  |

## Aggregate NuGet packages details

| Package | Current Version | Suggested Version | Projects | Description |
| :--- | :---: | :---: | :--- | :--- |
| AnimatedGif | 1.0.5 |  | [Animation.csproj](#animationanimationanimationcsproj) | ✅Compatible |
| coverlet.collector | 6.0.4 |  | [FDEMTests.csproj](#fdemtestsfdemtestscsproj) | ✅Compatible |
| Delaunator | 1.0.11 |  | [FDEMCore.csproj](#fdemcorefdemcorecsproj)<br/>[FxTMeshGenerator.csproj](#fxtmeshgeneratorfxtmeshgeneratorcsproj) | ✅Compatible |
| Microsoft.DotNet.UpgradeAssistant.Extensions.Default.Analyzers | 0.4.421302 |  | [RandomRVEGenerator.csproj](#randomrvegeneratorrandomrvegeneratorcsproj) | ✅Compatible |
| Microsoft.NET.Test.Sdk | 17.14.1 |  | [FDEMTests.csproj](#fdemtestsfdemtestscsproj) | ✅Compatible |
| NUnit | 4.4.0 |  | [FDEMTests.csproj](#fdemtestsfdemtestscsproj) | ✅Compatible |
| NUnit3TestAdapter | 5.1.0 |  | [FDEMTests.csproj](#fdemtestsfdemtestscsproj) | ✅Compatible |
| SinglePlotZedGraph | 1.0.0 |  | [FDEMWindows.csproj](#fdemwindowsfdemwindowscsproj)<br/>[PlotFDEM.csproj](#plotfdemplotfdemcsproj) | ⚠️NuGet package is incompatible |
| StapletonMathPackage | 1.3.4 |  | [FDEMCore.csproj](#fdemcorefdemcorecsproj) | ✅Compatible |
| System.Drawing.Common | 9.0.8 | 10.0.5 | [Animation.csproj](#animationanimationanimationcsproj) | NuGet package upgrade is recommended |
| ZedGraph | 5.2.0 |  | [Animation.csproj](#animationanimationanimationcsproj)<br/>[PlotFDEM.csproj](#plotfdemplotfdemcsproj) | ✅Compatible |

## Top API Migration Challenges

### Technologies and Features

| Technology | Issues | Percentage | Migration Path |
| :--- | :---: | :---: | :--- |
| Windows Forms | 3758 | 86.5% | Windows Forms APIs for building Windows desktop applications with traditional Forms-based UI that are available in .NET on Windows. Enable Windows Desktop support: Option 1 (Recommended): Target net9.0-windows; Option 2: Add <UseWindowsDesktop>true</UseWindowsDesktop>; Option 3 (Legacy): Use Microsoft.NET.Sdk.WindowsDesktop SDK. |
| GDI+ / System.Drawing | 582 | 13.4% | System.Drawing APIs for 2D graphics, imaging, and printing that are available via NuGet package System.Drawing.Common. Note: Not recommended for server scenarios due to Windows dependencies; consider cross-platform alternatives like SkiaSharp or ImageSharp for new code. |
| Legacy Configuration System | 4 | 0.1% | Legacy XML-based configuration system (app.config/web.config) that has been replaced by a more flexible configuration model in .NET Core. The old system was rigid and XML-based. Migrate to Microsoft.Extensions.Configuration with JSON/environment variables; use System.Configuration.ConfigurationManager NuGet package as interim bridge if needed. |
| Windows Forms Legacy Controls | 3 | 0.1% | Legacy Windows Forms controls that have been removed from .NET Core/5+ including StatusBar, DataGrid, ContextMenu, MainMenu, MenuItem, and ToolBar. These controls were replaced by more modern alternatives. Use ToolStrip, MenuStrip, ContextMenuStrip, and DataGridView instead. |
| Deprecated Remoting & Serialization | 1 | 0.0% | Legacy .NET Remoting, BinaryFormatter, and related serialization APIs that are deprecated and removed for security reasons. Remoting provided distributed object communication but had significant security vulnerabilities. Migrate to gRPC, HTTP APIs, or modern serialization (System.Text.Json, protobuf). |

### Most Frequent API Issues

| API | Count | Percentage | Category |
| :--- | :---: | :---: | :--- |
| T:System.Windows.Forms.Button | 434 | 10.0% | Binary Incompatible |
| T:System.Windows.Forms.Label | 312 | 7.2% | Binary Incompatible |
| T:System.Windows.Forms.ComboBox | 127 | 2.9% | Binary Incompatible |
| P:System.Windows.Forms.Control.Name | 106 | 2.4% | Binary Incompatible |
| T:System.Drawing.ContentAlignment | 102 | 2.3% | Source Incompatible |
| T:System.Windows.Forms.NumericUpDown | 100 | 2.3% | Binary Incompatible |
| P:System.Windows.Forms.Control.Size | 99 | 2.3% | Binary Incompatible |
| T:System.Windows.Forms.Control.ControlCollection | 97 | 2.2% | Binary Incompatible |
| P:System.Windows.Forms.Control.Controls | 97 | 2.2% | Binary Incompatible |
| M:System.Windows.Forms.Control.ControlCollection.Add(System.Windows.Forms.Control) | 96 | 2.2% | Binary Incompatible |
| P:System.Windows.Forms.Control.TabIndex | 96 | 2.2% | Binary Incompatible |
| P:System.Windows.Forms.Control.Location | 96 | 2.2% | Binary Incompatible |
| T:System.Windows.Forms.CheckBox | 93 | 2.1% | Binary Incompatible |
| T:System.Windows.Forms.Padding | 92 | 2.1% | Binary Incompatible |
| T:System.Windows.Forms.SplitContainer | 80 | 1.8% | Binary Incompatible |
| T:System.Windows.Forms.RadioButton | 55 | 1.3% | Binary Incompatible |
| T:System.Windows.Forms.DialogResult | 54 | 1.2% | Binary Incompatible |
| T:System.Windows.Forms.SplitterPanel | 51 | 1.2% | Binary Incompatible |
| T:System.Windows.Forms.GroupBox | 47 | 1.1% | Binary Incompatible |
| M:System.Windows.Forms.Padding.#ctor(System.Int32,System.Int32,System.Int32,System.Int32) | 46 | 1.1% | Binary Incompatible |
| T:System.Drawing.Graphics | 44 | 1.0% | Source Incompatible |
| P:System.Windows.Forms.ButtonBase.UseVisualStyleBackColor | 44 | 1.0% | Binary Incompatible |
| P:System.Windows.Forms.Control.Margin | 44 | 1.0% | Binary Incompatible |
| T:System.Windows.Forms.TrackBar | 39 | 0.9% | Binary Incompatible |
| P:System.Windows.Forms.SplitContainer.Panel1 | 39 | 0.9% | Binary Incompatible |
| T:System.Windows.Forms.TextBox | 37 | 0.9% | Binary Incompatible |
| T:System.Windows.Forms.Panel | 34 | 0.8% | Binary Incompatible |
| T:System.Windows.Forms.ContextMenuStrip | 34 | 0.8% | Binary Incompatible |
| E:System.Windows.Forms.Control.Click | 33 | 0.8% | Binary Incompatible |
| M:System.Windows.Forms.Button.#ctor | 33 | 0.8% | Binary Incompatible |
| T:System.Windows.Forms.ControlStyles | 32 | 0.7% | Binary Incompatible |
| P:System.Windows.Forms.ButtonBase.Text | 31 | 0.7% | Binary Incompatible |
| P:System.Windows.Forms.Label.Text | 30 | 0.7% | Binary Incompatible |
| P:System.Windows.Forms.ButtonBase.BackColor | 29 | 0.7% | Binary Incompatible |
| M:System.Windows.Forms.Label.#ctor | 27 | 0.6% | Binary Incompatible |
| T:System.Drawing.Drawing2D.SmoothingMode | 24 | 0.6% | Source Incompatible |
| T:System.Windows.Forms.AnchorStyles | 24 | 0.6% | Binary Incompatible |
| P:System.Windows.Forms.Label.TextAlign | 24 | 0.6% | Binary Incompatible |
| T:System.Windows.Forms.FlatStyle | 24 | 0.6% | Binary Incompatible |
| T:System.Windows.Forms.ImageLayout | 24 | 0.6% | Binary Incompatible |
| T:System.Windows.Forms.ListBox | 24 | 0.6% | Binary Incompatible |
| P:System.Windows.Forms.ComboBox.SelectedIndex | 23 | 0.5% | Binary Incompatible |
| T:System.Drawing.Bitmap | 22 | 0.5% | Source Incompatible |
| T:System.Windows.Forms.ToolStripMenuItem | 21 | 0.5% | Binary Incompatible |
| T:System.Windows.Forms.AutoScaleMode | 21 | 0.5% | Binary Incompatible |
| T:System.Drawing.Drawing2D.PixelOffsetMode | 21 | 0.5% | Source Incompatible |
| T:System.Drawing.Drawing2D.Matrix | 20 | 0.5% | Source Incompatible |
| F:System.Drawing.ContentAlignment.MiddleLeft | 17 | 0.4% | Source Incompatible |
| F:System.Drawing.ContentAlignment.MiddleRight | 17 | 0.4% | Source Incompatible |
| T:System.Drawing.Drawing2D.MatrixOrder | 16 | 0.4% | Source Incompatible |

## Projects Relationship Graph

Legend:
📦 SDK-style project
⚙️ Classic project

```mermaid
flowchart LR
    P1["<b>📦&nbsp;RandomRVEGenerator.csproj</b><br/><small>net5.0-windows</small>"]
    P2["<b>📦&nbsp;FDEMConsole.csproj</b><br/><small>net5.0</small>"]
    P3["<b>📦&nbsp;FDEMCore.csproj</b><br/><small>net5.0</small>"]
    P4["<b>📦&nbsp;FDEMWindows.csproj</b><br/><small>net5.0-windows</small>"]
    P5["<b>📦&nbsp;FDEMTests.csproj</b><br/><small>net10.0</small>"]
    P6["<b>📦&nbsp;RandomRVEGeneratorConsole.csproj</b><br/><small>net5.0</small>"]
    P7["<b>⚙️&nbsp;Animation.csproj</b><br/><small>net48</small>"]
    P8["<b>📦&nbsp;PlotFDEM.csproj</b><br/><small>net5.0-windows</small>"]
    P9["<b>📦&nbsp;FxTMeshGenerator.csproj</b><br/><small>net10.0</small>"]
    P1 --> P3
    P2 --> P3
    P4 --> P3
    P5 --> P3
    P5 --> P9
    P6 --> P3
    P8 --> P2
    P8 --> P7
    P9 --> P3
    click P1 "#randomrvegeneratorrandomrvegeneratorcsproj"
    click P2 "#fdemconsolefdemconsolecsproj"
    click P3 "#fdemcorefdemcorecsproj"
    click P4 "#fdemwindowsfdemwindowscsproj"
    click P5 "#fdemtestsfdemtestscsproj"
    click P6 "#randomrvegeneratorconsolerandomrvegeneratorconsolecsproj"
    click P7 "#animationanimationanimationcsproj"
    click P8 "#plotfdemplotfdemcsproj"
    click P9 "#fxtmeshgeneratorfxtmeshgeneratorcsproj"

```

## Project Details

<a id="animationanimationanimationcsproj"></a>
### animation\animation\Animation.csproj

#### Project Info

- **Current Target Framework:** net48
- **Proposed Target Framework:** net10.0-windows
- **SDK-style**: False
- **Project Kind:** ClassicWinForms
- **Dependencies**: 0
- **Dependants**: 1
- **Number of Files**: 6
- **Number of Files with Incidents**: 4
- **Lines of Code**: 1284
- **Estimated LOC to modify**: 1034+ (at least 80.5% of the project)

#### Dependency Graph

Legend:
📦 SDK-style project
⚙️ Classic project

```mermaid
flowchart TB
    subgraph upstream["Dependants (1)"]
        P8["<b>📦&nbsp;PlotFDEM.csproj</b><br/><small>net5.0-windows</small>"]
        click P8 "#plotfdemplotfdemcsproj"
    end
    subgraph current["Animation.csproj"]
        MAIN["<b>⚙️&nbsp;Animation.csproj</b><br/><small>net48</small>"]
        click MAIN "#animationanimationanimationcsproj"
    end
    P8 --> MAIN

```

### API Compatibility

| Category | Count | Impact |
| :--- | :---: | :--- |
| 🔴 Binary Incompatible | 906 | High - Require code changes |
| 🟡 Source Incompatible | 128 | Medium - Needs re-compilation and potential conflicting API error fixing |
| 🔵 Behavioral change | 0 | Low - Behavioral changes that may require testing at runtime |
| ✅ Compatible | 937 |  |
| ***Total APIs Analyzed*** | ***1971*** |  |

#### Project Technologies and Features

| Technology | Issues | Percentage | Migration Path |
| :--- | :---: | :---: | :--- |
| Windows Forms Legacy Controls | 1 | 0.1% | Legacy Windows Forms controls that have been removed from .NET Core/5+ including StatusBar, DataGrid, ContextMenu, MainMenu, MenuItem, and ToolBar. These controls were replaced by more modern alternatives. Use ToolStrip, MenuStrip, ContextMenuStrip, and DataGridView instead. |
| GDI+ / System.Drawing | 128 | 12.4% | System.Drawing APIs for 2D graphics, imaging, and printing that are available via NuGet package System.Drawing.Common. Note: Not recommended for server scenarios due to Windows dependencies; consider cross-platform alternatives like SkiaSharp or ImageSharp for new code. |
| Windows Forms | 906 | 87.6% | Windows Forms APIs for building Windows desktop applications with traditional Forms-based UI that are available in .NET on Windows. Enable Windows Desktop support: Option 1 (Recommended): Target net9.0-windows; Option 2: Add <UseWindowsDesktop>true</UseWindowsDesktop>; Option 3 (Legacy): Use Microsoft.NET.Sdk.WindowsDesktop SDK. |

<a id="fdemconsolefdemconsolecsproj"></a>
### FDEMConsole\FDEMConsole.csproj

#### Project Info

- **Current Target Framework:** net5.0
- **Proposed Target Framework:** net10.0
- **SDK-style**: True
- **Project Kind:** DotNetCoreApp
- **Dependencies**: 1
- **Dependants**: 1
- **Number of Files**: 1
- **Number of Files with Incidents**: 1
- **Lines of Code**: 128
- **Estimated LOC to modify**: 0+ (at least 0.0% of the project)

#### Dependency Graph

Legend:
📦 SDK-style project
⚙️ Classic project

```mermaid
flowchart TB
    subgraph upstream["Dependants (1)"]
        P8["<b>📦&nbsp;PlotFDEM.csproj</b><br/><small>net5.0-windows</small>"]
        click P8 "#plotfdemplotfdemcsproj"
    end
    subgraph current["FDEMConsole.csproj"]
        MAIN["<b>📦&nbsp;FDEMConsole.csproj</b><br/><small>net5.0</small>"]
        click MAIN "#fdemconsolefdemconsolecsproj"
    end
    subgraph downstream["Dependencies (1"]
        P3["<b>📦&nbsp;FDEMCore.csproj</b><br/><small>net5.0</small>"]
        click P3 "#fdemcorefdemcorecsproj"
    end
    P8 --> MAIN
    MAIN --> P3

```

### API Compatibility

| Category | Count | Impact |
| :--- | :---: | :--- |
| 🔴 Binary Incompatible | 0 | High - Require code changes |
| 🟡 Source Incompatible | 0 | Medium - Needs re-compilation and potential conflicting API error fixing |
| 🔵 Behavioral change | 0 | Low - Behavioral changes that may require testing at runtime |
| ✅ Compatible | 105 |  |
| ***Total APIs Analyzed*** | ***105*** |  |

<a id="fdemcorefdemcorecsproj"></a>
### FDEMCore\FDEMCore.csproj

#### Project Info

- **Current Target Framework:** net5.0
- **Proposed Target Framework:** net10.0
- **SDK-style**: True
- **Project Kind:** ClassLibrary
- **Dependencies**: 0
- **Dependants**: 6
- **Number of Files**: 34
- **Number of Files with Incidents**: 2
- **Lines of Code**: 11531
- **Estimated LOC to modify**: 1+ (at least 0.0% of the project)

#### Dependency Graph

Legend:
📦 SDK-style project
⚙️ Classic project

```mermaid
flowchart TB
    subgraph upstream["Dependants (6)"]
        P1["<b>📦&nbsp;RandomRVEGenerator.csproj</b><br/><small>net5.0-windows</small>"]
        P2["<b>📦&nbsp;FDEMConsole.csproj</b><br/><small>net5.0</small>"]
        P4["<b>📦&nbsp;FDEMWindows.csproj</b><br/><small>net5.0-windows</small>"]
        P5["<b>📦&nbsp;FDEMTests.csproj</b><br/><small>net10.0</small>"]
        P6["<b>📦&nbsp;RandomRVEGeneratorConsole.csproj</b><br/><small>net5.0</small>"]
        P9["<b>📦&nbsp;FxTMeshGenerator.csproj</b><br/><small>net10.0</small>"]
        click P1 "#randomrvegeneratorrandomrvegeneratorcsproj"
        click P2 "#fdemconsolefdemconsolecsproj"
        click P4 "#fdemwindowsfdemwindowscsproj"
        click P5 "#fdemtestsfdemtestscsproj"
        click P6 "#randomrvegeneratorconsolerandomrvegeneratorconsolecsproj"
        click P9 "#fxtmeshgeneratorfxtmeshgeneratorcsproj"
    end
    subgraph current["FDEMCore.csproj"]
        MAIN["<b>📦&nbsp;FDEMCore.csproj</b><br/><small>net5.0</small>"]
        click MAIN "#fdemcorefdemcorecsproj"
    end
    P1 --> MAIN
    P2 --> MAIN
    P4 --> MAIN
    P5 --> MAIN
    P6 --> MAIN
    P9 --> MAIN

```

### API Compatibility

| Category | Count | Impact |
| :--- | :---: | :--- |
| 🔴 Binary Incompatible | 0 | High - Require code changes |
| 🟡 Source Incompatible | 1 | Medium - Needs re-compilation and potential conflicting API error fixing |
| 🔵 Behavioral change | 0 | Low - Behavioral changes that may require testing at runtime |
| ✅ Compatible | 11528 |  |
| ***Total APIs Analyzed*** | ***11529*** |  |

#### Project Technologies and Features

| Technology | Issues | Percentage | Migration Path |
| :--- | :---: | :---: | :--- |
| Deprecated Remoting & Serialization | 1 | 100.0% | Legacy .NET Remoting, BinaryFormatter, and related serialization APIs that are deprecated and removed for security reasons. Remoting provided distributed object communication but had significant security vulnerabilities. Migrate to gRPC, HTTP APIs, or modern serialization (System.Text.Json, protobuf). |

<a id="fdemtestsfdemtestscsproj"></a>
### FDEMTests\FDEMTests.csproj

#### Project Info

- **Current Target Framework:** net10.0✅
- **SDK-style**: True
- **Project Kind:** DotNetCoreApp
- **Dependencies**: 2
- **Dependants**: 0
- **Number of Files**: 26
- **Lines of Code**: 4564
- **Estimated LOC to modify**: 0+ (at least 0.0% of the project)

#### Dependency Graph

Legend:
📦 SDK-style project
⚙️ Classic project

```mermaid
flowchart TB
    subgraph current["FDEMTests.csproj"]
        MAIN["<b>📦&nbsp;FDEMTests.csproj</b><br/><small>net10.0</small>"]
        click MAIN "#fdemtestsfdemtestscsproj"
    end
    subgraph downstream["Dependencies (2"]
        P3["<b>📦&nbsp;FDEMCore.csproj</b><br/><small>net5.0</small>"]
        P9["<b>📦&nbsp;FxTMeshGenerator.csproj</b><br/><small>net10.0</small>"]
        click P3 "#fdemcorefdemcorecsproj"
        click P9 "#fxtmeshgeneratorfxtmeshgeneratorcsproj"
    end
    MAIN --> P3
    MAIN --> P9

```

### API Compatibility

| Category | Count | Impact |
| :--- | :---: | :--- |
| 🔴 Binary Incompatible | 0 | High - Require code changes |
| 🟡 Source Incompatible | 0 | Medium - Needs re-compilation and potential conflicting API error fixing |
| 🔵 Behavioral change | 0 | Low - Behavioral changes that may require testing at runtime |
| ✅ Compatible | 0 |  |
| ***Total APIs Analyzed*** | ***0*** |  |

<a id="fdemwindowsfdemwindowscsproj"></a>
### FDEMWindows\FDEMWindows.csproj

#### Project Info

- **Current Target Framework:** net5.0-windows
- **Proposed Target Framework:** net10.0-windows
- **SDK-style**: True
- **Project Kind:** WinForms
- **Dependencies**: 1
- **Dependants**: 0
- **Number of Files**: 2
- **Number of Files with Incidents**: 3
- **Lines of Code**: 84
- **Estimated LOC to modify**: 22+ (at least 26.2% of the project)

#### Dependency Graph

Legend:
📦 SDK-style project
⚙️ Classic project

```mermaid
flowchart TB
    subgraph current["FDEMWindows.csproj"]
        MAIN["<b>📦&nbsp;FDEMWindows.csproj</b><br/><small>net5.0-windows</small>"]
        click MAIN "#fdemwindowsfdemwindowscsproj"
    end
    subgraph downstream["Dependencies (1"]
        P3["<b>📦&nbsp;FDEMCore.csproj</b><br/><small>net5.0</small>"]
        click P3 "#fdemcorefdemcorecsproj"
    end
    MAIN --> P3

```

### API Compatibility

| Category | Count | Impact |
| :--- | :---: | :--- |
| 🔴 Binary Incompatible | 20 | High - Require code changes |
| 🟡 Source Incompatible | 2 | Medium - Needs re-compilation and potential conflicting API error fixing |
| 🔵 Behavioral change | 0 | Low - Behavioral changes that may require testing at runtime |
| ✅ Compatible | 19 |  |
| ***Total APIs Analyzed*** | ***41*** |  |

#### Project Technologies and Features

| Technology | Issues | Percentage | Migration Path |
| :--- | :---: | :---: | :--- |
| Legacy Configuration System | 2 | 9.1% | Legacy XML-based configuration system (app.config/web.config) that has been replaced by a more flexible configuration model in .NET Core. The old system was rigid and XML-based. Migrate to Microsoft.Extensions.Configuration with JSON/environment variables; use System.Configuration.ConfigurationManager NuGet package as interim bridge if needed. |
| Windows Forms | 20 | 90.9% | Windows Forms APIs for building Windows desktop applications with traditional Forms-based UI that are available in .NET on Windows. Enable Windows Desktop support: Option 1 (Recommended): Target net9.0-windows; Option 2: Add <UseWindowsDesktop>true</UseWindowsDesktop>; Option 3 (Legacy): Use Microsoft.NET.Sdk.WindowsDesktop SDK. |

<a id="fxtmeshgeneratorfxtmeshgeneratorcsproj"></a>
### FxTMeshGenerator\FxTMeshGenerator.csproj

#### Project Info

- **Current Target Framework:** net10.0✅
- **SDK-style**: True
- **Project Kind:** DotNetCoreApp
- **Dependencies**: 1
- **Dependants**: 1
- **Number of Files**: 19
- **Lines of Code**: 2469
- **Estimated LOC to modify**: 0+ (at least 0.0% of the project)

#### Dependency Graph

Legend:
📦 SDK-style project
⚙️ Classic project

```mermaid
flowchart TB
    subgraph upstream["Dependants (1)"]
        P5["<b>📦&nbsp;FDEMTests.csproj</b><br/><small>net10.0</small>"]
        click P5 "#fdemtestsfdemtestscsproj"
    end
    subgraph current["FxTMeshGenerator.csproj"]
        MAIN["<b>📦&nbsp;FxTMeshGenerator.csproj</b><br/><small>net10.0</small>"]
        click MAIN "#fxtmeshgeneratorfxtmeshgeneratorcsproj"
    end
    subgraph downstream["Dependencies (1"]
        P3["<b>📦&nbsp;FDEMCore.csproj</b><br/><small>net5.0</small>"]
        click P3 "#fdemcorefdemcorecsproj"
    end
    P5 --> MAIN
    MAIN --> P3

```

### API Compatibility

| Category | Count | Impact |
| :--- | :---: | :--- |
| 🔴 Binary Incompatible | 0 | High - Require code changes |
| 🟡 Source Incompatible | 0 | Medium - Needs re-compilation and potential conflicting API error fixing |
| 🔵 Behavioral change | 0 | Low - Behavioral changes that may require testing at runtime |
| ✅ Compatible | 0 |  |
| ***Total APIs Analyzed*** | ***0*** |  |

<a id="plotfdemplotfdemcsproj"></a>
### PlotFDEM\PlotFDEM.csproj

#### Project Info

- **Current Target Framework:** net5.0-windows
- **Proposed Target Framework:** net10.0-windows
- **SDK-style**: True
- **Project Kind:** WinForms
- **Dependencies**: 2
- **Dependants**: 0
- **Number of Files**: 29
- **Number of Files with Incidents**: 18
- **Lines of Code**: 5871
- **Estimated LOC to modify**: 3215+ (at least 54.8% of the project)

#### Dependency Graph

Legend:
📦 SDK-style project
⚙️ Classic project

```mermaid
flowchart TB
    subgraph current["PlotFDEM.csproj"]
        MAIN["<b>📦&nbsp;PlotFDEM.csproj</b><br/><small>net5.0-windows</small>"]
        click MAIN "#plotfdemplotfdemcsproj"
    end
    subgraph downstream["Dependencies (2"]
        P2["<b>📦&nbsp;FDEMConsole.csproj</b><br/><small>net5.0</small>"]
        P7["<b>⚙️&nbsp;Animation.csproj</b><br/><small>net48</small>"]
        click P2 "#fdemconsolefdemconsolecsproj"
        click P7 "#animationanimationanimationcsproj"
    end
    MAIN --> P2
    MAIN --> P7

```

### API Compatibility

| Category | Count | Impact |
| :--- | :---: | :--- |
| 🔴 Binary Incompatible | 2761 | High - Require code changes |
| 🟡 Source Incompatible | 454 | Medium - Needs re-compilation and potential conflicting API error fixing |
| 🔵 Behavioral change | 0 | Low - Behavioral changes that may require testing at runtime |
| ✅ Compatible | 6207 |  |
| ***Total APIs Analyzed*** | ***9422*** |  |

#### Project Technologies and Features

| Technology | Issues | Percentage | Migration Path |
| :--- | :---: | :---: | :--- |
| Windows Forms Legacy Controls | 2 | 0.1% | Legacy Windows Forms controls that have been removed from .NET Core/5+ including StatusBar, DataGrid, ContextMenu, MainMenu, MenuItem, and ToolBar. These controls were replaced by more modern alternatives. Use ToolStrip, MenuStrip, ContextMenuStrip, and DataGridView instead. |
| GDI+ / System.Drawing | 454 | 14.1% | System.Drawing APIs for 2D graphics, imaging, and printing that are available via NuGet package System.Drawing.Common. Note: Not recommended for server scenarios due to Windows dependencies; consider cross-platform alternatives like SkiaSharp or ImageSharp for new code. |
| Windows Forms | 2761 | 85.9% | Windows Forms APIs for building Windows desktop applications with traditional Forms-based UI that are available in .NET on Windows. Enable Windows Desktop support: Option 1 (Recommended): Target net9.0-windows; Option 2: Add <UseWindowsDesktop>true</UseWindowsDesktop>; Option 3 (Legacy): Use Microsoft.NET.Sdk.WindowsDesktop SDK. |

<a id="randomrvegeneratorrandomrvegeneratorcsproj"></a>
### RandomRVEGenerator\RandomRVEGenerator.csproj

#### Project Info

- **Current Target Framework:** net5.0-windows
- **Proposed Target Framework:** net10.0-windows
- **SDK-style**: True
- **Project Kind:** WinForms
- **Dependencies**: 1
- **Dependants**: 0
- **Number of Files**: 8
- **Number of Files with Incidents**: 5
- **Lines of Code**: 332
- **Estimated LOC to modify**: 73+ (at least 22.0% of the project)

#### Dependency Graph

Legend:
📦 SDK-style project
⚙️ Classic project

```mermaid
flowchart TB
    subgraph current["RandomRVEGenerator.csproj"]
        MAIN["<b>📦&nbsp;RandomRVEGenerator.csproj</b><br/><small>net5.0-windows</small>"]
        click MAIN "#randomrvegeneratorrandomrvegeneratorcsproj"
    end
    subgraph downstream["Dependencies (1"]
        P3["<b>📦&nbsp;FDEMCore.csproj</b><br/><small>net5.0</small>"]
        click P3 "#fdemcorefdemcorecsproj"
    end
    MAIN --> P3

```

### API Compatibility

| Category | Count | Impact |
| :--- | :---: | :--- |
| 🔴 Binary Incompatible | 71 | High - Require code changes |
| 🟡 Source Incompatible | 2 | Medium - Needs re-compilation and potential conflicting API error fixing |
| 🔵 Behavioral change | 0 | Low - Behavioral changes that may require testing at runtime |
| ✅ Compatible | 145 |  |
| ***Total APIs Analyzed*** | ***218*** |  |

#### Project Technologies and Features

| Technology | Issues | Percentage | Migration Path |
| :--- | :---: | :---: | :--- |
| Legacy Configuration System | 2 | 2.7% | Legacy XML-based configuration system (app.config/web.config) that has been replaced by a more flexible configuration model in .NET Core. The old system was rigid and XML-based. Migrate to Microsoft.Extensions.Configuration with JSON/environment variables; use System.Configuration.ConfigurationManager NuGet package as interim bridge if needed. |
| Windows Forms | 71 | 97.3% | Windows Forms APIs for building Windows desktop applications with traditional Forms-based UI that are available in .NET on Windows. Enable Windows Desktop support: Option 1 (Recommended): Target net9.0-windows; Option 2: Add <UseWindowsDesktop>true</UseWindowsDesktop>; Option 3 (Legacy): Use Microsoft.NET.Sdk.WindowsDesktop SDK. |

<a id="randomrvegeneratorconsolerandomrvegeneratorconsolecsproj"></a>
### RandomRVEGeneratorConsole\RandomRVEGeneratorConsole.csproj

#### Project Info

- **Current Target Framework:** net5.0
- **Proposed Target Framework:** net10.0
- **SDK-style**: True
- **Project Kind:** DotNetCoreApp
- **Dependencies**: 1
- **Dependants**: 0
- **Number of Files**: 1
- **Number of Files with Incidents**: 1
- **Lines of Code**: 106
- **Estimated LOC to modify**: 0+ (at least 0.0% of the project)

#### Dependency Graph

Legend:
📦 SDK-style project
⚙️ Classic project

```mermaid
flowchart TB
    subgraph current["RandomRVEGeneratorConsole.csproj"]
        MAIN["<b>📦&nbsp;RandomRVEGeneratorConsole.csproj</b><br/><small>net5.0</small>"]
        click MAIN "#randomrvegeneratorconsolerandomrvegeneratorconsolecsproj"
    end
    subgraph downstream["Dependencies (1"]
        P3["<b>📦&nbsp;FDEMCore.csproj</b><br/><small>net5.0</small>"]
        click P3 "#fdemcorefdemcorecsproj"
    end
    MAIN --> P3

```

### API Compatibility

| Category | Count | Impact |
| :--- | :---: | :--- |
| 🔴 Binary Incompatible | 0 | High - Require code changes |
| 🟡 Source Incompatible | 0 | Medium - Needs re-compilation and potential conflicting API error fixing |
| 🔵 Behavioral change | 0 | Low - Behavioral changes that may require testing at runtime |
| ✅ Compatible | 105 |  |
| ***Total APIs Analyzed*** | ***105*** |  |

