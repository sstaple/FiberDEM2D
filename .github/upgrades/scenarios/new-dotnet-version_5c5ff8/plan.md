# .NET 10.0 Upgrade Migration Plan

**Date:** January 2025  
**Solution:** FiberDEM.sln  
**Source Branch:** main  
**Target Branch:** upgrade-to-NET10  
**Current Frameworks:** .NET Framework 4.8, .NET 5.0, .NET 10.0  
**Target Framework:** .NET 10.0 (Long Term Support)

---

## 🎯 UPDATED STRATEGY: Quick Wins First

**This plan has been updated to prioritize immediate, low-risk migrations:**

### Immediate Scope (Phase 1-2)
✅ **Migrate 4 .NET 5.0 projects to .NET 10.0** (1-2 days)
- FDEMCore.csproj (core library)
- FDEMConsole.csproj (console app)
- RandomRVEGeneratorConsole.csproj (console app)
- RandomRVEGenerator.csproj (WinForms app)

**Why this approach?**
- Low-hanging fruit: Already SDK-style, minimal complexity
- Quick wins: Build momentum and validate approach
- Low risk: Straightforward framework retargeting

### Deferred Work (Phase 3 - Future)
⏸️ **3 projects deferred to separate efforts:**
- Animation.csproj (requires SDK conversion from .NET Framework 4.8)
- FDEMWindows.csproj (blocked by SinglePlotZedGraph package)
- PlotFDEM.csproj (blocked by Animation + SinglePlotZedGraph)

**Why deferred?**
- Complex SDK conversion needed
- Package incompatibility must be researched (ScottPlot/OxyPlot alternatives)
- Can tackle separately after Phase 1-2 success

### Future Enhancements Noted
🔮 Replace ZedGraph charting library (ScottPlot, OxyPlot, or LiveCharts)  
🔮 Consider MAUI migration for GUI components  
🔮 Complete .NET Framework to .NET Core migration

---

## Executive Summary

This plan outlines the migration of the FiberDEM solution from mixed framework versions (.NET Framework 4.8 and .NET 5.0) to **.NET 10.0 (LTS)**. The solution contains **9 projects** with varying complexity levels, including Windows Forms applications and console applications.

### Key Highlights

- **Total Projects:** 9 (4 to upgrade now, 3 deferred, 2 already complete)
- **Immediate Scope:** 4 .NET 5.0 projects → .NET 10.0
- **Deferred Scope:** 3 projects (blocked by SDK conversion or package incompatibility)
- **Total LOC:** 26,369
- **Estimated LOC to Modify (Phase 1-2):** ~75+ (minimal - mostly .NET 5.0 clean upgrades)
- **Total API Issues (Deferred to Phase 3):** 4,356 (handled in separate effort)
- **Package Updates Required (Now):** 0 (System.Drawing and ZedGraph deferred)
- **Critical Dependencies:** Windows Forms APIs, System.Drawing (GDI+) - mostly deferred
- **Estimated Timeline (Phase 1-2):** 1-2 days for .NET 5.0 conversions
- **Risk Level (Phase 1-2):** 🟢 Low (straightforward framework upgrades)

### Migration Strategy: **Incremental Migration - Quick Wins First**

**Rationale:**
- Solution has 9 projects with complex dependency relationships
- Significant API compatibility issues (4,356 total incidents)
- 16.5% of codebase requires modification
- Mix of project types (WinForms, Console, ClassLibrary, Test projects)
- One project requires SDK-style conversion (.NET Framework 4.8 → .NET 10.0)
- Incremental approach reduces risk and allows for staged testing

**UPDATED STRATEGY (User-Driven):**
- **Phase 1-2:** Focus on .NET 5.0 projects first (low-hanging fruit, quick wins)
- **Defer complex work:** Animation.csproj SDK conversion and SinglePlotZedGraph resolution
- **Future considerations:** Replace ZedGraph with modern library (ScottPlot/OxyPlot), potential MAUI migration
- **Benefits:** Build momentum, validate approach, defer blockers until later

---

## Migration Strategy Justification

### Why Incremental Migration with Quick Wins First?

1. **Complexity:** 9 projects with dependency chains requiring careful sequencing
2. **API Breaking Changes:** 3,758 binary incompatible API calls require code changes
3. **SDK-Style Conversion:** Animation.csproj must be converted from classic to SDK-style
4. **Testing Requirements:** Each phase can be tested independently before proceeding
5. **Risk Mitigation:** Allows rollback at phase boundaries without losing all progress
6. **Windows Forms Complexity:** 86.5% of issues are Windows Forms-related, requiring careful validation

### Why Start with .NET 5.0 Projects? (UPDATED STRATEGY)

**Strategic Benefits:**
1. ✅ **Quick Wins** - Build momentum with straightforward upgrades
2. ✅ **Low Risk** - Already SDK-style, no project format conversion needed
3. ✅ **Validate Approach** - Test migration strategy on simpler projects first
4. ✅ **Immediate Value** - Get most projects to modern framework quickly
5. ✅ **Defer Blockers** - Handle complex issues (SDK conversion, package incompatibilities) separately

**What We're Deferring:**
- ⏸️ Animation.csproj SDK conversion (.NET Framework 4.8 → .NET 10.0)
- ⏸️ SinglePlotZedGraph package resolution (replace with ScottPlot/OxyPlot later)
- ⏸️ FDEMWindows & PlotFDEM (blocked by SinglePlotZedGraph)

**Future Enhancements Noted:**
- 🔮 Consider MAUI migration for GUI components
- 🔮 Replace ZedGraph charting with modern alternative
- 🔮 Complete SDK conversion of remaining .NET Framework projects

---

## Dependency Analysis

### Project Dependency Order (Bottom-Up) - UPDATED STRATEGY

**Strategy Update:** Focus on quick wins by upgrading .NET 5.0 projects first. Defer complex projects requiring SDK conversion or package resolution until later phases.

Based on the dependency graph from the assessment, projects will be migrated in the following order:

```
Phase 1 - Quick Wins: .NET 5.0 Console/Library Projects (NO BLOCKERS)
  ├─ FDEMCore.csproj (net5.0 → net10.0) [Core library - 6 dependents]
  ├─ FDEMConsole.csproj (net5.0 → net10.0) [Clean upgrade]
  └─ RandomRVEGeneratorConsole.csproj (net5.0 → net10.0) [Clean upgrade]

Phase 2 - .NET 5.0 WinForms Projects (NO PACKAGE BLOCKERS)
  └─ RandomRVEGenerator.csproj (net5.0-windows → net10.0-windows)
     [71 API issues but no critical blockers]

Phase 3 - DEFERRED: Projects with Blockers
  ├─ Animation.csproj (net48 → net10.0-windows) 
     ⏸️ DEFERRED: Requires SDK-style conversion (complex)
  ├─ FDEMWindows.csproj (net5.0-windows → net10.0-windows)
     ⏸️ DEFERRED: Blocked by SinglePlotZedGraph incompatibility
  └─ PlotFDEM.csproj (net5.0-windows → net10.0-windows)
     ⏸️ DEFERRED: Blocked by Animation + SinglePlotZedGraph

Phase 4 - Already Complete
  ├─ FxTMeshGenerator.csproj (Already net10.0 ✅)
  └─ FDEMTests.csproj (Already net10.0 ✅)
```

### Dependency Justification - REVISED

**Immediate Priorities (Phase 1 & 2):**
- **FDEMCore** must go first as 6 other projects depend on it (1 minor serialization issue)
- **Console projects** are clean upgrades with no complications
- **RandomRVEGenerator** can proceed independently without package blockers

**Deferred Items (Phase 3):**
- **Animation.csproj** - Complex SDK conversion from .NET Framework 4.8, tackle separately
- **FDEMWindows & PlotFDEM** - Both blocked by SinglePlotZedGraph package incompatibility
- **Strategy:** Handle ZedGraph replacement in separate effort (possibly ScottPlot or OxyPlot)
- **Future Consideration:** MAUI migration for GUI components

**Already Complete (Phase 4):**
- **FDEMTests & FxTMeshGenerator** - Already on .NET 10.0, will validate Phase 1 & 2 work

---

## Package Update Reference

### Packages Requiring Updates

| Package | Current Version | Target Version | Affected Projects | Priority | Reason |
|---------|-----------------|----------------|-------------------|----------|---------|
| **System.Drawing.Common** | 9.0.8 | **10.0.5** | Animation.csproj | High | Upgrade recommended for .NET 10.0 compatibility |
| **SinglePlotZedGraph** | 1.0.0 | *Investigate* | FDEMWindows.csproj, PlotFDEM.csproj | Critical | ⚠️ **Incompatible with .NET 10.0** - Needs replacement or alternative |

### Packages Marked as Compatible (No Action Required)

- AnimatedGif 1.0.5 ✅
- coverlet.collector 6.0.4 ✅
- Delaunator 1.0.11 ✅
- Microsoft.DotNet.UpgradeAssistant.Extensions.Default.Analyzers 0.4.421302 ✅
- Microsoft.NET.Test.Sdk 17.14.1 ✅
- NUnit 4.4.0 ✅
- NUnit3TestAdapter 5.1.0 ✅
- StapletonMathPackage 1.3.4 ✅
- ZedGraph 5.2.0 ✅

### Critical Package Issue: SinglePlotZedGraph

**Problem:** SinglePlotZedGraph 1.0.0 is marked as incompatible with .NET 10.0

**Investigation Required:**
1. Check if a newer version exists compatible with .NET 10.0
2. Investigate if source code is available for modification
3. Consider replacing with compatible alternative (e.g., OxyPlot, ScottPlot, LiveCharts)
4. Assess impact on FDEMWindows and PlotFDEM functionality

**Mitigation Options:**
- **Option A:** Find/upgrade to compatible version
- **Option B:** Replace with modern charting library (ScottPlot, OxyPlot)
- **Option C:** Fork and upgrade the package source if available
- **Option D:** Remove dependency and implement minimal custom plotting

**Decision Point:** This must be resolved before migrating Phase 2 projects that depend on it.

---

## Breaking Changes Catalog

### Major Breaking Change Categories

#### 1. Windows Forms APIs (86.5% of issues - 3,758 incidents)

**Impact:** High - All WinForms projects affected

**Affected Projects:**
- Animation.csproj (906 incidents)
- FDEMWindows.csproj (20 incidents)
- PlotFDEM.csproj (2,761 incidents)
- RandomRVEGenerator.csproj (71 incidents)

**Nature of Breaking Changes:**
- Binary incompatibility requiring recompilation
- Most issues will be auto-resolved by retargeting to `net10.0-windows`
- Designer-generated code will regenerate correctly
- Manual intervention may be needed for:
  - Custom control implementations
  - P/Invoke declarations
  - Unsafe code blocks

**Migration Approach:**
1. Update target framework to `net10.0-windows`
2. Rebuild and let designer regenerate `.Designer.cs` files
3. Address compiler errors for custom implementations
4. Test UI rendering and functionality thoroughly

**Known Frequent Issues:**
- `System.Windows.Forms.Button` (434 usages)
- `System.Windows.Forms.Label` (312 usages)
- `System.Windows.Forms.ComboBox` (127 usages)
- `System.Windows.Forms.Control` properties (Size, Location, Name, etc.)

#### 2. GDI+ / System.Drawing (13.4% of issues - 582 incidents)

**Impact:** Medium - Available via NuGet package

**Affected Projects:**
- Animation.csproj (128 incidents)
- PlotFDEM.csproj (454 incidents)

**Nature of Breaking Changes:**
- Source incompatibility requiring package reference
- APIs moved to `System.Drawing.Common` NuGet package

**Migration Approach:**
1. Ensure `System.Drawing.Common` package is referenced (upgrade to 10.0.5)
2. Verify all usings are correct
3. Consider adding `System.Drawing.EnableUnixSupport` in runtimeconfig if cross-platform support needed

**Known Issues:**
- `System.Drawing.Graphics` (44 usages)
- `System.Drawing.Bitmap` (22 usages)
- `System.Drawing.ContentAlignment` (102 usages)
- `System.Drawing.Drawing2D.*` types (SmoothingMode, Matrix, etc.)

#### 3. Windows Forms Legacy Controls (0.1% - 3 incidents)

**Impact:** Low - Rare usage

**Affected Projects:**
- Animation.csproj (1 incident)
- PlotFDEM.csproj (2 incidents)

**Controls Removed:**
- StatusBar → Replace with StatusStrip
- DataGrid → Replace with DataGridView
- ContextMenu → Replace with ContextMenuStrip
- MainMenu/MenuItem → Replace with MenuStrip/ToolStripMenuItem
- ToolBar → Replace with ToolStrip

**Migration Approach:**
1. Identify specific legacy controls used
2. Replace with modern equivalents
3. Update event handlers and property references
4. Test functionality

#### 4. Legacy Configuration System (0.1% - 4 incidents)

**Impact:** Low - Minimal usage

**Affected Projects:**
- FDEMWindows.csproj (2 incidents)
- RandomRVEGenerator.csproj (2 incidents)

**Breaking Changes:**
- `System.Configuration.ConfigurationManager` removed from framework
- `app.config` handling changed

**Migration Approach:**
1. Add `System.Configuration.ConfigurationManager` NuGet package (interim bridge)
2. Consider migrating to `Microsoft.Extensions.Configuration` for modern approach
3. Move from XML to JSON configuration files if feasible

#### 5. Deprecated Remoting & Serialization (0.0% - 1 incident)

**Impact:** Very Low - Single usage

**Affected Projects:**
- FDEMCore.csproj (1 incident)

**Breaking Changes:**
- BinaryFormatter deprecated and removed for security reasons
- .NET Remoting APIs removed

**Migration Approach:**
1. Identify specific serialization usage
2. Replace with:
   - `System.Text.Json` for JSON serialization
   - `protobuf-net` for binary serialization
   - gRPC for distributed communication
3. Update any custom serialization logic

---

## SDK-Style Project Conversion

### Animation.csproj Conversion Plan

**Current State:** Classic .NET Framework 4.8 project (non-SDK-style)  
**Target State:** SDK-style .NET 10.0-windows project

**Why Conversion is Required:**
- Modern .NET (Core/5+) requires SDK-style project format
- Classic format not supported in .NET 10.0

**Conversion Steps:**

1. **Backup Current Project File**
   ```bash
   Copy-Item "animation\animation\Animation.csproj" "animation\animation\Animation.csproj.backup"
   ```

2. **Use Upgrade Assistant or Manual Conversion**
   
   **Option A: Automated (Recommended)**
   ```bash
   upgrade-assistant upgrade animation\animation\Animation.csproj --target-tfm-support lts
   ```

   **Option B: Manual Conversion**
   - Create new SDK-style project structure
   - Migrate project references
   - Migrate package references
   - Migrate embedded resources
   - Update namespace imports

3. **Expected New Project Structure**
   ```xml
   <Project Sdk="Microsoft.NET.Sdk">
     <PropertyGroup>
       <TargetFramework>net10.0-windows</TargetFramework>
       <OutputType>Library</OutputType>
       <UseWindowsForms>true</UseWindowsForms>
     </PropertyGroup>
     
     <ItemGroup>
       <PackageReference Include="AnimatedGif" Version="1.0.5" />
       <PackageReference Include="System.Drawing.Common" Version="10.0.5" />
       <PackageReference Include="ZedGraph" Version="5.2.0" />
     </ItemGroup>
   </Project>
   ```

4. **Validation After Conversion**
   - [ ] Project loads in Visual Studio
   - [ ] All files are included (check for missing items)
   - [ ] References are resolved
   - [ ] Project builds without errors
   - [ ] Designer files open correctly

**Risk Level:** Medium
- SDK conversion can have unexpected issues
- Designer compatibility must be verified
- File inclusions may differ (SDK-style is inclusive by default)

---

## Phase-by-Phase Migration Plan - UPDATED FOR QUICK WINS

### Phase 1: Foundation - .NET 5.0 Core Library & Console Projects

**Objective:** Upgrade the core library and console applications (quick wins, no blockers)

#### Phase 1.1: FDEMCore.csproj

**Project Details:**
- **Current TFM:** net5.0
- **Target TFM:** net10.0
- **Project Type:** ClassLibrary (SDK-style)
- **LOC:** 11,531
- **Estimated Impact:** 1+ LOC (minimal)
- **Dependencies:** None
- **Dependents:** 6 projects
- **Risk Level:** 🟢 Low

**Issues to Address:**
- 1 source incompatible API (Deprecated Remoting & Serialization)
  - Likely `BinaryFormatter` or related serialization API
  - Must be replaced with modern serialization

**Migration Steps:**

1. **Update Target Framework**
   ```xml
   <TargetFramework>net10.0</TargetFramework>
   ```

2. **Identify Serialization Usage**
   - Search for `BinaryFormatter`, `ISerializable`, `SerializationInfo`
   - Locate files containing the 1 source incompatible API

3. **Replace Deprecated Serialization**
   - **If BinaryFormatter:** Replace with `System.Text.Json`
   - **If ISerializable:** Implement JSON serialization contracts
   - Example:
     ```csharp
     // Old (Deprecated)
     BinaryFormatter formatter = new BinaryFormatter();
     formatter.Serialize(stream, data);

     // New (Recommended)
     JsonSerializer.Serialize(stream, data);
     ```

4. **Build and Validate**
   ```bash
   dotnet build FDEMCore\FDEMCore.csproj
   ```

5. **Run Tests** (if applicable)
   ```bash
   dotnet test FDEMTests\FDEMTests.csproj --filter "Category=FDEMCore"
   ```

**Success Criteria:**
- [ ] Project builds without errors
- [ ] Project builds without warnings
- [ ] No serialization-related runtime errors
- [ ] All dependent projects still reference FDEMCore correctly
- [ ] Unit tests pass (if available)

**Rollback Plan:** Revert `<TargetFramework>` to net5.0

**Estimated Time:** 2-4 hours

---

#### Phase 1.2: FDEMConsole.csproj

**Project Details:**
- **Current TFM:** net5.0
- **Target TFM:** net10.0
- **Project Type:** DotNetCoreApp (SDK-style)
- **LOC:** 128
- **Estimated Impact:** 0+ LOC (minimal)
- **Dependencies:** FDEMCore
- **Dependents:** PlotFDEM (deferred)
- **Risk Level:** 🟢 Low

**Issues to Address:** None (clean upgrade)

**Migration Steps:**

1. **Update Target Framework**
   ```xml
   <TargetFramework>net10.0</TargetFramework>
   ```

2. **Build**
   ```bash
   dotnet build FDEMConsole\FDEMConsole.csproj
   ```

3. **Test Console Application**
   - Run application with test inputs
   - Verify expected outputs

**Success Criteria:**
- [ ] Project builds without errors
- [ ] Project builds without warnings
- [ ] Application runs successfully
- [ ] Console output is correct

**Rollback Plan:** Revert `<TargetFramework>` to net5.0

**Estimated Time:** 1-2 hours

---

#### Phase 1.3: RandomRVEGeneratorConsole.csproj

**Project Details:**
- **Current TFM:** net5.0
- **Target TFM:** net10.0
- **Project Type:** DotNetCoreApp (SDK-style)
- **LOC:** 106
- **Estimated Impact:** 0+ LOC (minimal)
- **Dependencies:** FDEMCore
- **Dependents:** None
- **Risk Level:** 🟢 Low

**Issues to Address:** None (clean upgrade)

**Migration Steps:**

1. **Update Target Framework**
   ```xml
   <TargetFramework>net10.0</TargetFramework>
   ```

2. **Build**
   ```bash
   dotnet build RandomRVEGeneratorConsole\RandomRVEGeneratorConsole.csproj
   ```

3. **Test Console Application**
   - Run application with test inputs
   - Verify RVE generation output

**Success Criteria:**
- [ ] Project builds without errors
- [ ] Project builds without warnings
- [ ] Application runs successfully
- [ ] Output is correct

**Rollback Plan:** Revert `<TargetFramework>` to net5.0

**Estimated Time:** 1-2 hours

---

### Phase 2: .NET 5.0 WinForms Project (No Package Blockers)

**Objective:** Upgrade WinForms project without critical package dependencies

#### Phase 2.1: RandomRVEGenerator.csproj

**Project Details:**
- **Current TFM:** net5.0-windows
- **Target TFM:** net10.0-windows
- **Project Type:** WinForms (SDK-style)
- **LOC:** 332
- **Estimated Impact:** 73+ LOC (22.0%)
- **Dependencies:** FDEMCore
- **Dependents:** None
- **Risk Level:** 🟡 Medium

**Issues to Address:**
- **71 binary incompatible APIs** (Windows Forms)
- **2 source incompatible APIs** (Legacy Configuration)

**Migration Steps:**

1. **Add Configuration Package**
   ```xml
   <PackageReference Include="System.Configuration.ConfigurationManager" Version="10.0.0" />
   ```

2. **Update Target Framework**
   ```xml
   <TargetFramework>net10.0-windows</TargetFramework>
   <UseWindowsForms>true</UseWindowsForms>
   ```

3. **Build and Address Errors**
   ```bash
   dotnet build RandomRVEGenerator\RandomRVEGenerator.csproj
   ```

4. **Fix Configuration Access**
   - Update `app.config` references
   - Migrate to JSON configuration if feasible

5. **Regenerate Designer Files**
   - Open forms in Visual Studio Designer
   - Allow designer to regenerate `.Designer.cs` files

6. **Test Application**
   - Launch application
   - Test RVE generation functionality
   - Verify random number generation
   - Test export functionality

**Success Criteria:**
- [ ] Project builds without errors
- [ ] Project builds without warnings
- [ ] Application launches successfully
- [ ] Configuration loading works
- [ ] RVE generation works correctly
- [ ] Export functionality works

**Rollback Plan:** Revert `<TargetFramework>` to net5.0-windows

**Estimated Time:** 4-6 hours

---

### Phase 3: DEFERRED - Complex Projects

**Status:** ⏸️ These projects are intentionally deferred to separate efforts

#### Phase 3.1: Animation.csproj ⏸️ DEFERRED

**Project Details:**
- **Current TFM:** net48
- **Target TFM:** net10.0-windows
- **Project Type:** ClassicWinForms → WinForms (SDK-style)
- **LOC:** 1,284
- **Estimated Impact:** 1,034+ LOC (80.5%)
- **Dependencies:** None
- **Dependents:** PlotFDEM.csproj
- **Risk Level:** 🟡 Medium

**Why Deferred:**
- ⚠️ Requires SDK-style conversion from classic .NET Framework 4.8
- Complex project format migration
- Will tackle in separate effort after Phase 1-2 complete

**Future Work:**
- Follow "SDK-Style Project Conversion" section when ready
- Use `upgrade-assistant` tool for automated conversion
- Consider part of broader .NET Framework migration effort

---

#### Phase 3.2: FDEMWindows.csproj ⏸️ DEFERRED

**Project Details:**
- **Current TFM:** net5.0-windows
- **Target TFM:** net10.0-windows
- **Project Type:** WinForms (SDK-style)
- **LOC:** 84
- **Risk Level:** 🟢 Low (once blocker resolved)

**Why Deferred:**
- ⚠️ **Blocked by SinglePlotZedGraph** incompatibility
- Must resolve charting library issue first

**Future Work:**
- Replace SinglePlotZedGraph with ScottPlot, OxyPlot, or LiveCharts
- See "Appendix B: Package Resolution for SinglePlotZedGraph"
- Straightforward once package resolved

---

#### Phase 3.3: PlotFDEM.csproj ⏸️ DEFERRED - HIGHEST COMPLEXITY

**Project Details:**
- **Current TFM:** net5.0-windows
- **Target TFM:** net10.0-windows
- **Project Type:** WinForms (SDK-style)
- **LOC:** 5,871
- **Estimated Impact:** 3,215+ LOC (54.8%)
- **Dependencies:** FDEMConsole, Animation
- **Dependents:** None
- **Risk Level:** 🔴 High

**Why Deferred:**
- ⚠️ **Depends on Animation.csproj** (deferred - SDK conversion needed)
- ⚠️ **Blocked by SinglePlotZedGraph** incompatibility
- Largest and most complex project (29 forms, 2,761 API issues)

**Future Work:**
- Complete after Animation.csproj conversion
- Complete after SinglePlotZedGraph replacement
- Most extensive testing required
- Consider breaking into sub-tasks by form/module

---

### Phase 4: Already Complete ✅

#### Phase 4.1: FxTMeshGenerator.csproj ✅

**Project Details:**
- **Current TFM:** net10.0 ✅ Already upgraded!
- **No action required**

---

#### Phase 4.2: FDEMTests.csproj ✅

**Project Details:**
- **Current TFM:** net10.0 ✅ Already upgraded!
- **No action required**
- Will be used to validate Phase 1 & 2 migrations

**Testing Plan:**
1. Run after Phase 1 completes to validate FDEMCore
2. Run after Phase 2 completes to ensure no regressions
3. Provides confidence before tackling deferred projects

**Test Execution:**
```bash
dotnet test FDEMTests\FDEMTests.csproj
```

**Expected:** All tests pass after Phase 1-2 migrations complete

**Objective:** Upgrade the core library that all other projects depend on

**Project Details:**
- **Current TFM:** net5.0
- **Target TFM:** net10.0
- **Project Type:** ClassLibrary (SDK-style)
- **LOC:** 11,531
- **Estimated Impact:** 1+ LOC (minimal)
- **Dependencies:** None
- **Dependents:** 6 projects
- **Risk Level:** 🟢 Low

**Issues to Address:**
- 1 source incompatible API (Deprecated Remoting & Serialization)
  - Likely `BinaryFormatter` or related serialization API
  - Must be replaced with modern serialization

**Migration Steps:**

1. **Update Target Framework**
   ```xml
   <TargetFramework>net10.0</TargetFramework>
   ```

2. **Identify Serialization Usage**
   - Search for `BinaryFormatter`, `ISerializable`, `SerializationInfo`
   - Locate files containing the 1 source incompatible API

3. **Replace Deprecated Serialization**
   - **If BinaryFormatter:** Replace with `System.Text.Json`
   - **If ISerializable:** Implement JSON serialization contracts
   - Example:
     ```csharp
     // Old (Deprecated)
     BinaryFormatter formatter = new BinaryFormatter();
     formatter.Serialize(stream, data);
     
     // New (Recommended)
     JsonSerializer.Serialize(stream, data);
     ```

4. **Build and Validate**
   ```bash
   dotnet build FDEMCore\FDEMCore.csproj
   ```

5. **Run Tests** (if applicable)
   ```bash
   dotnet test FDEMTests\FDEMTests.csproj --filter "Category=FDEMCore"
   ```

**Success Criteria:**
- [ ] Project builds without errors
- [ ] Project builds without warnings
- [ ] No serialization-related runtime errors
- [ ] All dependent projects still reference FDEMCore correctly
- [ ] Unit tests pass (if available)

**Rollback Plan:** Revert `<TargetFramework>` to net5.0

**Estimated Time:** 2-4 hours

---

### Phase 2: Independent Projects

**Objective:** Upgrade projects that depend only on FDEMCore

#### Phase 2.1: Animation.csproj ⚠️ HIGH COMPLEXITY

**Project Details:**
- **Current TFM:** net48
- **Target TFM:** net10.0-windows
- **Project Type:** ClassicWinForms → WinForms (SDK-style)
- **LOC:** 1,284
- **Estimated Impact:** 1,034+ LOC (80.5%)
- **Dependencies:** None
- **Dependents:** PlotFDEM.csproj
- **Risk Level:** 🟡 Medium

**Critical Prerequisite:** SDK-style conversion (see section above)

**Issues to Address:**
- **906 binary incompatible APIs** (Windows Forms)
- **128 source incompatible APIs** (System.Drawing)
- **1 legacy control** to replace
- **1 package upgrade:** System.Drawing.Common 9.0.8 → 10.0.5

**Migration Steps:**

1. **Convert to SDK-Style**
   - Follow "SDK-Style Project Conversion" section above
   - Use `upgrade-assistant` tool for automated conversion

2. **Update Target Framework**
   ```xml
   <TargetFramework>net10.0-windows</TargetFramework>
   <UseWindowsForms>true</UseWindowsForms>
   ```

3. **Update Package References**
   ```xml
   <PackageReference Include="System.Drawing.Common" Version="10.0.5" />
   <PackageReference Include="AnimatedGif" Version="1.0.5" />
   <PackageReference Include="ZedGraph" Version="5.2.0" />
   ```

4. **Build and Address Errors**
   ```bash
   dotnet build animation\animation\Animation.csproj
   ```

5. **Fix Legacy Controls**
   - Search for `StatusBar`, `DataGrid`, `ContextMenu`, `MainMenu`, `MenuItem`, `ToolBar`
   - Replace with modern equivalents (StatusStrip, DataGridView, ContextMenuStrip, MenuStrip)

6. **Regenerate Designer Files**
   - Open forms in Visual Studio Designer
   - Allow designer to regenerate `.Designer.cs` files
   - Verify no layout issues

7. **Test UI Functionality**
   - Open and interact with all forms
   - Verify animations render correctly
   - Test GIF generation functionality

**Success Criteria:**
- [ ] SDK-style conversion successful
- [ ] Project builds without errors
- [ ] Project builds without warnings
- [ ] All forms open in designer
- [ ] UI renders correctly at runtime
- [ ] Animation functionality works
- [ ] GIF export works
- [ ] No legacy controls remain

**Rollback Plan:** 
- Restore from `Animation.csproj.backup`
- Revert dependent project changes

**Estimated Time:** 8-12 hours (includes SDK conversion and testing)

---

#### Phase 2.2: FDEMConsole.csproj

**Project Details:**
- **Current TFM:** net5.0
- **Target TFM:** net10.0
- **Project Type:** DotNetCoreApp (SDK-style)
- **LOC:** 128
- **Estimated Impact:** 0+ LOC (minimal)
- **Dependencies:** FDEMCore
- **Dependents:** PlotFDEM
- **Risk Level:** 🟢 Low

**Issues to Address:** None (clean upgrade)

**Migration Steps:**

1. **Update Target Framework**
   ```xml
   <TargetFramework>net10.0</TargetFramework>
   ```

2. **Build**
   ```bash
   dotnet build FDEMConsole\FDEMConsole.csproj
   ```

3. **Test Console Application**
   - Run application with test inputs
   - Verify expected outputs

**Success Criteria:**
- [ ] Project builds without errors
- [ ] Project builds without warnings
- [ ] Application runs successfully
- [ ] Console output is correct

**Rollback Plan:** Revert `<TargetFramework>` to net5.0

**Estimated Time:** 1-2 hours

---

#### Phase 2.3: FDEMWindows.csproj

**Project Details:**
- **Current TFM:** net5.0-windows
- **Target TFM:** net10.0-windows
- **Project Type:** WinForms (SDK-style)
- **LOC:** 84
- **Estimated Impact:** 22+ LOC (26.2%)
- **Dependencies:** FDEMCore
- **Dependents:** None
- **Risk Level:** 🟢 Low

**Issues to Address:**
- **20 binary incompatible APIs** (Windows Forms)
- **2 source incompatible APIs** (Legacy Configuration)
- **1 incompatible package:** SinglePlotZedGraph ⚠️

**Critical Blocker:** SinglePlotZedGraph incompatibility must be resolved first

**Migration Steps:**

1. **Resolve SinglePlotZedGraph Dependency** (Decision required)
   - Investigate package alternatives
   - Document chosen approach in plan comments
   - Implement replacement if needed

2. **Add Configuration Package** (for legacy config support)
   ```xml
   <PackageReference Include="System.Configuration.ConfigurationManager" Version="10.0.0" />
   ```

3. **Update Target Framework**
   ```xml
   <TargetFramework>net10.0-windows</TargetFramework>
   <UseWindowsForms>true</UseWindowsForms>
   ```

4. **Build and Address Errors**
   ```bash
   dotnet build FDEMWindows\FDEMWindows.csproj
   ```

5. **Fix Configuration Access**
   - Update `app.config` references
   - Migrate to JSON configuration if feasible

6. **Test Application**
   - Launch WinForms application
   - Verify UI rendering
   - Test plotting functionality (once SinglePlotZedGraph resolved)

**Success Criteria:**
- [ ] SinglePlotZedGraph issue resolved
- [ ] Project builds without errors
- [ ] Project builds without warnings
- [ ] Application launches successfully
- [ ] Configuration loading works
- [ ] Plotting works (if applicable)

**Rollback Plan:** Revert `<TargetFramework>` to net5.0-windows

**Estimated Time:** 4-8 hours (depends on SinglePlotZedGraph resolution)

---

#### Phase 2.4: RandomRVEGenerator.csproj

**Project Details:**
- **Current TFM:** net5.0-windows
- **Target TFM:** net10.0-windows
- **Project Type:** WinForms (SDK-style)
- **LOC:** 332
- **Estimated Impact:** 73+ LOC (22.0%)
- **Dependencies:** FDEMCore
- **Dependents:** None
- **Risk Level:** 🟡 Medium

**Issues to Address:**
- **71 binary incompatible APIs** (Windows Forms)
- **2 source incompatible APIs** (Legacy Configuration)

**Migration Steps:**

1. **Add Configuration Package**
   ```xml
   <PackageReference Include="System.Configuration.ConfigurationManager" Version="10.0.0" />
   ```

2. **Update Target Framework**
   ```xml
   <TargetFramework>net10.0-windows</TargetFramework>
   <UseWindowsForms>true</UseWindowsForms>
   ```

3. **Build and Address Errors**
   ```bash
   dotnet build RandomRVEGenerator\RandomRVEGenerator.csproj
   ```

4. **Fix Configuration Access**
   - Update `app.config` references
   - Migrate to JSON configuration if feasible

5. **Regenerate Designer Files**
   - Open forms in Visual Studio Designer
   - Allow designer to regenerate `.Designer.cs` files

6. **Test Application**
   - Launch application
   - Test RVE generation functionality
   - Verify random number generation
   - Test export functionality

**Success Criteria:**
- [ ] Project builds without errors
- [ ] Project builds without warnings
- [ ] Application launches successfully
- [ ] Configuration loading works
- [ ] RVE generation works correctly
- [ ] Export functionality works

**Rollback Plan:** Revert `<TargetFramework>` to net5.0-windows

**Estimated Time:** 4-6 hours

---

#### Phase 2.5: RandomRVEGeneratorConsole.csproj

**Project Details:**
- **Current TFM:** net5.0
- **Target TFM:** net10.0
- **Project Type:** DotNetCoreApp (SDK-style)
- **LOC:** 106
- **Estimated Impact:** 0+ LOC (minimal)
- **Dependencies:** FDEMCore
- **Dependents:** None
- **Risk Level:** 🟢 Low

**Issues to Address:** None (clean upgrade)

**Migration Steps:**

1. **Update Target Framework**
   ```xml
   <TargetFramework>net10.0</TargetFramework>
   ```

2. **Build**
   ```bash
   dotnet build RandomRVEGeneratorConsole\RandomRVEGeneratorConsole.csproj
   ```

3. **Test Console Application**
   - Run application with test inputs
   - Verify RVE generation output

**Success Criteria:**
- [ ] Project builds without errors
- [ ] Project builds without warnings
- [ ] Application runs successfully
- [ ] Output is correct

**Rollback Plan:** Revert `<TargetFramework>` to net5.0

**Estimated Time:** 1-2 hours

---

#### Phase 2.6: FxTMeshGenerator.csproj ✅

**Project Details:**
- **Current TFM:** net10.0 ✅ Already upgraded!
- **No action required**

---

### Phase 3: Dependent Projects

#### Phase 3.1: PlotFDEM.csproj ⚠️ HIGHEST COMPLEXITY

**Project Details:**
- **Current TFM:** net5.0-windows
- **Target TFM:** net10.0-windows
- **Project Type:** WinForms (SDK-style)
- **LOC:** 5,871
- **Estimated Impact:** 3,215+ LOC (54.8%)
- **Dependencies:** FDEMConsole, Animation
- **Dependents:** None
- **Risk Level:** 🔴 High

**Critical Dependencies:**
- Requires Animation.csproj to be migrated first (Phase 2.1)
- Requires FDEMConsole.csproj to be migrated first (Phase 2.2)

**Issues to Address:**
- **2,761 binary incompatible APIs** (Windows Forms) - HIGHEST COUNT
- **454 source incompatible APIs** (System.Drawing) - HIGHEST COUNT
- **2 legacy controls** to replace
- **1 incompatible package:** SinglePlotZedGraph ⚠️

**Critical Blocker:** SinglePlotZedGraph incompatibility must be resolved (same as FDEMWindows)

**Migration Steps:**

1. **Verify Prerequisites**
   - [ ] Animation.csproj successfully migrated
   - [ ] FDEMConsole.csproj successfully migrated
   - [ ] SinglePlotZedGraph resolution decided and implemented

2. **Update Target Framework**
   ```xml
   <TargetFramework>net10.0-windows</TargetFramework>
   <UseWindowsForms>true</UseWindowsForms>
   ```

3. **Update Package References**
   ```xml
   <PackageReference Include="ZedGraph" Version="5.2.0" />
   <!-- SinglePlotZedGraph replacement or alternative here -->
   ```

4. **Build and Address Errors**
   ```bash
   dotnet build PlotFDEM\PlotFDEM.csproj
   ```
   
   **Expected:** Many compilation errors initially (3,215+ API incidents)

5. **Fix Legacy Controls** (Priority 1)
   - Search for `StatusBar`, `DataGrid`, `ContextMenu`, `MainMenu`, `MenuItem`, `ToolBar`
   - Replace with modern equivalents

6. **Regenerate Designer Files** (Priority 2)
   - Open all 29 forms in Visual Studio Designer one by one
   - Allow designer to regenerate `.Designer.cs` files
   - This should auto-resolve most Windows Forms API issues

7. **Fix System.Drawing Issues** (Priority 3)
   - Address remaining 454 source incompatible drawing APIs
   - Ensure `System.Drawing.Common` package referenced
   - Update any custom drawing code

8. **Address Custom Control Issues** (Priority 4)
   - Fix any custom control implementations
   - Update event handlers
   - Fix property bindings

9. **Test Extensively** (Priority 5)
   - Open and test all 29 forms
   - Test plotting functionality thoroughly
   - Test data visualization
   - Test animation integration
   - Test export/import functionality
   - Performance testing

**Success Criteria:**
- [ ] Project builds without errors
- [ ] Project builds without warnings
- [ ] All 29 forms open in designer
- [ ] All forms render correctly at runtime
- [ ] Plotting functionality works
- [ ] Animation integration works
- [ ] Data export/import works
- [ ] Performance is acceptable
- [ ] No legacy controls remain

**Rollback Plan:** 
- Revert `<TargetFramework>` to net5.0-windows
- May need to revert Animation and FDEMConsole as well

**Estimated Time:** 16-24 hours (largest and most complex project)

**Risk Mitigation:**
- Break into smaller sub-tasks by form/module
- Test incrementally after each form fix
- Commit frequently for granular rollback
- Consider creating separate feature branch for this project

---

### Phase 4: Test Projects

#### Phase 4.1: FDEMTests.csproj ✅

**Project Details:**
- **Current TFM:** net10.0 ✅ Already upgraded!
- **No action required**
- Will be used to validate all previous phase migrations

**Testing Plan:**
1. Run after Phase 1 completes to validate FDEMCore
2. Run after Phase 2 completes to validate FxTMeshGenerator
3. Run after all phases to validate entire solution

**Test Execution:**
```bash
dotnet test FDEMTests\FDEMTests.csproj
```

**Expected:** All tests pass after migrations complete

---

## Testing Strategy - UPDATED FOR PHASE 1-2

### Multi-Level Testing Approach

#### Level 1: Per-Project Testing (Phase 1-2)

**Execute after each individual project migration**

**Build Validation:**
```bash
dotnet build <ProjectPath> --configuration Release
```

**Checks:**
- [ ] No compilation errors
- [ ] No compilation warnings
- [ ] All dependencies resolve correctly
- [ ] Output assemblies generated

**Project-Specific Tests:**
- **FDEMCore:** Run unit tests, validate serialization changes
- **Console Apps:** Execute with sample inputs, verify outputs
- **RandomRVEGenerator:** Launch UI, verify rendering, test RVE generation

---

#### Level 2: Phase Testing (Phase 1-2 Only)

**Execute after completing Phase 1 and Phase 2**

**Phase 1 (Core & Console Projects) Validation:**
```bash
# Build all dependent projects
dotnet build FiberDEM.sln --configuration Release

# Run tests
dotnet test FDEMTests\FDEMTests.csproj
```

**Checks:**
- [ ] Solution builds successfully
- [ ] All projects resolve FDEMCore reference correctly
- [ ] FDEMTests pass
- [ ] Console applications run without errors
- [ ] No serialization issues in consuming projects

**Phase 2 (RandomRVEGenerator) Validation:**
```bash
# Build entire solution
dotnet build FiberDEM.sln --configuration Release

# Run automated tests
dotnet test FDEMTests\FDEMTests.csproj

# Manual testing:
# - Launch RandomRVEGenerator and verify UI
# - Test RVE generation functionality
# - Verify configuration loading
# - Test export functionality
```

**Checks:**
- [ ] RandomRVEGenerator builds successfully
- [ ] Application launches without errors
- [ ] UI rendering is correct
- [ ] Configuration loading works
- [ ] Core functionality verified
- [ ] No regressions in RVE generation

---

#### Level 3: Phase 1-2 Completion Testing

**Execute after all Phase 1-2 projects complete**

**Build Validation:**
```bash
# Clean and rebuild entire solution
dotnet clean FiberDEM.sln
dotnet build FiberDEM.sln --configuration Release
dotnet build FiberDEM.sln --configuration Debug
```

**Test Execution:**
```bash
# Run all automated tests
dotnet test FiberDEM.sln --logger "console;verbosity=detailed"
```

**Integration Testing Checklist (Limited Scope - Phase 1-2):**
- [ ] FDEMCore serialization works in all consuming projects
- [ ] Console applications integrate correctly with FDEMCore
- [ ] RandomRVEGenerator works correctly with FDEMCore
- [ ] Configuration persistence works in RandomRVEGenerator
- [ ] Random RVE generation works in both GUI and console versions
- [ ] No behavioral changes in upgraded projects

**Regression Testing (Phase 1-2 Scope):**
- [ ] Compare outputs with .NET 5 version using identical inputs
- [ ] Verify no behavioral changes in calculations
- [ ] Performance comparison (should be same or better)
- [ ] RVE generation output matches previous version

**Key Scenarios for Phase 1-2:**
1. **Scenario 1: Console RVE Generation**
   - Generate RVE using RandomRVEGeneratorConsole
   - Verify output files are correct
   - Compare with .NET 5 baseline

2. **Scenario 2: GUI RVE Generation**
   - Generate RVE using RandomRVEGenerator (GUI)
   - Verify UI functionality
   - Test export functionality
   - Compare output with console version

3. **Scenario 3: Core Library Usage**
   - Verify FDEMCore works in FDEMTests
   - Verify serialization changes work correctly
   - Test with existing data files (if applicable)

**Performance Testing (Phase 1-2):**
```bash
# Benchmark key operations in upgraded projects
# - RVE generation performance (console and GUI)
# - FDEMCore operations performance
```

**Acceptance Criteria (Phase 1-2):**
- Performance within 5% of .NET 5 version
- All outputs match .NET 5 version
- No new warnings or errors in logs
- Memory usage similar to .NET 5 version

---

### Testing Checklist Summary (Phase 1-2)

#### Per-Project Checklist Template

For each project migrated in Phase 1-2, complete this checklist:

**Build:**
- [ ] Builds without errors
- [ ] Builds without warnings
- [ ] Dependencies resolve correctly
- [ ] Output assemblies generated

**Functionality:**
- [ ] Application launches successfully (if applicable)
- [ ] UI renders correctly (if WinForms)
- [ ] Core functionality works
- [ ] Configuration loads correctly (if applicable)
- [ ] Data I/O works

**Integration:**
- [ ] Dependent projects can reference it
- [ ] Unit tests pass (if available)
- [ ] No breaking changes to public API

**Documentation:**
- [ ] Changes documented
- [ ] Known issues documented
- [ ] Testing notes recorded

---

### Deferred Testing (Phase 3)

The following testing will be performed when deferred projects are addressed:

**Deferred Projects:**
- Animation.csproj - Full WinForms designer testing, GIF generation validation
- FDEMWindows.csproj - UI testing, charting with new library
- PlotFDEM.csproj - Extensive testing of 29 forms, plotting, animation integration

**Deferred Integration Scenarios:**
- Animation integration with PlotFDEM
- FDEMConsole integration with PlotFDEM
- Full end-to-end workflow testing
- Cross-application data exchange

**Deferred Performance Testing:**
- Animation generation performance
- Plotting performance with large datasets
- Full solution stress testing

---

## Risk Management

### Identified Risks and Mitigation Strategies

#### Risk 1: SinglePlotZedGraph Incompatibility

**Severity:** 🔴 Critical  
**Likelihood:** High  
**Impact:** Blocks FDEMWindows and PlotFDEM migration

**Affected Projects:**
- FDEMWindows.csproj
- PlotFDEM.csproj

**Mitigation Strategy:**
1. **Immediate Investigation (Before Phase 2):**
   - Check for updated version compatible with .NET 10.0
   - Search for source code repository
   - Research alternative charting libraries (ScottPlot, OxyPlot, LiveCharts)
   - Assess refactoring effort for each option

2. **Decision Matrix:**
   | Option | Effort | Risk | Timeline Impact |
   |--------|--------|------|-----------------|
   | Find compatible version | Low | Low | Minimal |
   | Fork and upgrade package | Medium | Medium | +1-2 days |
   | Replace with ScottPlot | High | Medium | +3-5 days |
   | Replace with OxyPlot | High | Medium | +3-5 days |
   | Custom implementation | Very High | High | +1-2 weeks |

3. **Contingency:**
   - If no solution found, temporarily comment out plotting functionality
   - Complete rest of migration
   - Address plotting as separate task

**Status:** 🚧 Requires immediate attention before Phase 2.3 and 3.1

---

#### Risk 2: Animation.csproj SDK Conversion Failure

**Severity:** 🟡 High  
**Likelihood:** Medium  
**Impact:** Blocks PlotFDEM migration

**Potential Issues:**
- Designer file regeneration errors
- Missing file inclusions
- Resource embedding issues
- Reference resolution problems

**Mitigation Strategy:**
1. **Prepare Backup:**
   - Create full backup of Animation project before conversion
   - Document current project structure
   - Save list of all files and references

2. **Use Upgrade Assistant:**
   - Use official `upgrade-assistant` tool (automated, reduces errors)
   - Follow tool's recommendations
   - Review changes carefully

3. **Manual Verification:**
   - Compare before/after file lists
   - Verify all references included
   - Check resource files embedded correctly
   - Test designer for each form

4. **Fallback Options:**
   - Manual conversion using documented patterns
   - Create new SDK-style project and migrate files incrementally
   - Seek community support on GitHub/Stack Overflow

**Rollback Plan:**
- Keep `Animation.csproj.backup` until PlotFDEM successfully migrated
- Document all changes made during conversion
- Revert dependent projects if rollback needed

---

#### Risk 3: Windows Forms API Breaking Changes

**Severity:** 🟡 Medium  
**Likelihood:** High  
**Impact:** Compilation errors, UI rendering issues

**Scale:**
- 3,758 binary incompatible API calls
- Affects 4 projects
- PlotFDEM has 2,761 incidents alone

**Mitigation Strategy:**
1. **Leverage Designer Auto-Fix:**
   - Most issues in `.Designer.cs` files will auto-resolve
   - Opening forms in designer regenerates code for .NET 10.0

2. **Incremental Approach:**
   - Fix one form at a time in PlotFDEM
   - Test each form after fixing
   - Commit working forms immediately

3. **Common Fix Patterns:**
   ```csharp
   // Most issues resolved by retargeting framework
   // Designer files auto-regenerate
   // Manual fixes needed only for custom controls
   ```

4. **Reference Documentation:**
   - Keep .NET breaking changes documentation handy
   - Reference .NET 5→6, 6→7, 7→8, 8→9, 9→10 breaking changes
   - Windows Forms-specific migration guides

**Expected Resolution:**
- 95% of issues auto-resolve via designer regeneration
- 5% require manual intervention for custom implementations

---

#### Risk 4: Legacy Configuration System Migration

**Severity:** 🟢 Low  
**Likelihood:** Low  
**Impact:** Configuration loading failures

**Affected Projects:**
- FDEMWindows.csproj (2 incidents)
- RandomRVEGenerator.csproj (2 incidents)

**Mitigation Strategy:**
1. **Interim Bridge:**
   - Add `System.Configuration.ConfigurationManager` NuGet package
   - Maintains backward compatibility with `app.config`
   - Minimal code changes required

2. **Optional Modernization:**
   - Consider migrating to `Microsoft.Extensions.Configuration`
   - Move to `appsettings.json` for better modern .NET integration
   - This is optional and can be deferred

**Expected Resolution:**
- Add NuGet package → issue resolved
- No significant code changes needed

---

#### Risk 5: Deprecated Serialization API

**Severity:** 🟢 Low  
**Likelihood:** Low  
**Impact:** Data serialization failures

**Affected Projects:**
- FDEMCore.csproj (1 incident)

**Mitigation Strategy:**
1. **Identify Usage:**
   - Locate specific BinaryFormatter or serialization usage
   - Understand data being serialized
   - Identify all serialize/deserialize call sites

2. **Replace with Modern Serialization:**
   ```csharp
   // Old (Deprecated)
   BinaryFormatter formatter = new BinaryFormatter();
   formatter.Serialize(stream, data);
   
   // New (Recommended)
   using System.Text.Json;
   JsonSerializer.Serialize(stream, data, options);
   ```

3. **Backward Compatibility:**
   - If existing serialized data exists, implement reader for old format
   - Add migration utility to convert old data
   - Maintain both readers temporarily during transition

4. **Testing:**
   - Test serialization round-trip
   - Verify data integrity
   - Test with existing serialized files

**Expected Resolution:**
- Straightforward replacement with `System.Text.Json`
- May need data migration if persisted data exists

---

#### Risk 6: Test Failures After Migration

**Severity:** 🟡 Medium  
**Likelihood:** Medium  
**Impact:** Regression in functionality

**Mitigation Strategy:**
1. **Baseline Tests:**
   - Run full test suite on .NET 5 before migration
   - Document all test results
   - Capture test coverage metrics

2. **Incremental Testing:**
   - Run tests after each phase
   - Isolate failures to specific changes
   - Fix before proceeding to next phase

3. **Manual Testing:**
   - Comprehensive manual testing checklist
   - Involve domain experts
   - Test edge cases and known problem areas

4. **Regression Prevention:**
   - Add tests for any new bugs found
   - Increase test coverage if gaps identified
   - Document test results for comparison

---

#### Risk 7: Performance Degradation

**Severity:** 🟡 Medium  
**Likelihood:** Low  
**Impact:** Slower application performance

**Mitigation Strategy:**
1. **Baseline Performance:**
   - Benchmark key operations on .NET 5
   - Document performance metrics
   - Identify performance-critical paths

2. **Performance Testing:**
   - Re-run benchmarks after migration
   - Compare .NET 10.0 vs .NET 5 performance
   - Identify any regressions

3. **.NET 10.0 Optimizations:**
   - Leverage new performance features
   - Review .NET 10.0 performance improvements
   - Apply recommended patterns

**Expected Outcome:**
- .NET 10.0 typically faster than .NET 5
- Should see performance improvements in most areas
- JIT improvements, GC improvements, span optimizations

---

### Risk Summary Matrix - UPDATED FOR PHASE 1-2

| Risk | Severity | Likelihood | Impact | Phase 1-2 Status |
|------|----------|------------|--------|------------------|
| SinglePlotZedGraph Incompatibility | 🔴 Critical | High | Blocks 2 projects | ⏸️ **DEFERRED to Phase 3** |
| Animation SDK Conversion | 🟡 High | Medium | Blocks PlotFDEM | ⏸️ **DEFERRED to Phase 3** |
| Windows Forms Breaking Changes | 🟡 Medium | High | Compilation errors | ⚠️ **Minimal in Phase 1-2** (71 in RandomRVEGenerator, auto-fixable) |
| Test Failures | 🟡 Medium | Medium | Regression risk | ✅ **Active in Phase 1-2** |
| Performance Degradation | 🟡 Medium | Low | User experience | ✅ **Active in Phase 1-2** (unlikely) |
| Legacy Configuration | 🟢 Low | Low | Config loading | ✅ **Active in Phase 1-2** (RandomRVEGenerator only, easy fix) |
| Deprecated Serialization | 🟢 Low | Low | Data persistence | ✅ **Active in Phase 1-2** (FDEMCore only, easy fix) |

**Phase 1-2 Risk Profile:** 🟢 **LOW** - Only 2 active risks, both low severity and easily mitigated  
**Phase 3 Risk Profile:** 🟡 **MEDIUM-HIGH** - Deferred risks will need attention in future work

---

## Rollback Strategy

### Phase-Level Rollback

Each phase can be independently rolled back without affecting previous phases:

**Phase 1 Rollback:**
```xml
<!-- FDEMCore.csproj -->
<TargetFramework>net5.0</TargetFramework>
```
- Revert serialization changes
- Rebuild dependent projects

**Phase 2 Rollback:**
- Revert each project's `<TargetFramework>` independently
- For Animation: restore from `Animation.csproj.backup`
- Remove added NuGet packages

**Phase 3 Rollback:**
```xml
<!-- PlotFDEM.csproj -->
<TargetFramework>net5.0-windows</TargetFramework>
```
- Revert package changes
- May need to revert Phase 2 projects if breaking changes in PlotFDEM dependencies

### Git Branch Strategy

**Recommended Branch Strategy:**

```
main (source branch)
  └─ upgrade-to-NET10 (target branch)
       ├─ phase-1-fdemcore
       ├─ phase-2-animation
       ├─ phase-2-console-apps
       ├─ phase-2-winforms-apps
       └─ phase-3-plotfdem
```

**Commit Strategy:**
- Commit after each project successfully migrated
- Tag each phase completion: `phase-1-complete`, `phase-2-complete`, etc.
- Enables rollback to any phase boundary

**Branch Merging:**
- Merge phase branches into `upgrade-to-NET10` after validation
- Keep phase branches until full solution tested
- Merge `upgrade-to-NET10` into `main` only after complete success

### Emergency Rollback

**If Critical Issue Arises:**

1. **Stop All Work Immediately**
2. **Identify Last Known Good State**
   - Last successful phase
   - Last successful commit
3. **Revert to Last Known Good**
   ```bash
   git checkout upgrade-to-NET10
   git reset --hard <last-good-commit-hash>
   ```
4. **Verify Rollback Success**
   ```bash
   dotnet build FiberDEM.sln
   dotnet test FDEMTests\FDEMTests.csproj
   ```
5. **Document Issue**
   - What went wrong
   - Why rollback was needed
   - Lessons learned
6. **Plan Fix**
   - Research issue
   - Develop solution
   - Create new attempt plan

---

## Success Criteria - UPDATED FOR PHASE 1-2

### Technical Success Criteria (Phase 1-2 Only)

The Phase 1-2 migration is considered technically successful when **ALL** of the following are met:

#### Build Success
- [ ] 4 upgraded projects build without errors (FDEMCore, FDEMConsole, RandomRVEGeneratorConsole, RandomRVEGenerator)
- [ ] 4 upgraded projects build without warnings (or only acceptable warnings documented)
- [ ] Solution builds in both Debug and Release configurations
- [ ] No NuGet package dependency conflicts

#### Framework Targets Met
- [ ] FDEMCore.csproj targets net10.0 ✅
- [ ] FDEMConsole.csproj targets net10.0 ✅
- [ ] RandomRVEGeneratorConsole.csproj targets net10.0 ✅
- [ ] RandomRVEGenerator.csproj targets net10.0-windows ✅
- [ ] FxTMeshGenerator.csproj targets net10.0 ✅ (already done)
- [ ] FDEMTests.csproj targets net10.0 ✅ (already done)

#### Package Updates Applied
- [ ] System.Configuration.ConfigurationManager added to RandomRVEGenerator (if needed)
- [ ] No new package vulnerabilities introduced

#### API Compatibility (Phase 1-2)
- [ ] 1 deprecated serialization API fixed in FDEMCore (BinaryFormatter)
- [ ] 71 Windows Forms APIs in RandomRVEGenerator resolved (via designer regeneration)
- [ ] 2 legacy configuration APIs in RandomRVEGenerator resolved (via ConfigurationManager package)

#### Testing Success
- [ ] All automated tests pass (FDEMTests)
- [ ] All 3 console applications run successfully
- [ ] RandomRVEGenerator WinForms application launches and renders correctly
- [ ] RVE generation works (GUI and console)
- [ ] Configuration loading works in RandomRVEGenerator

#### Performance Criteria
- [ ] Application startup time within 10% of .NET 5 version
- [ ] Core operations (RVE generation) within 5% of .NET 5 performance
- [ ] Memory usage not significantly increased (within 10%)

#### Integration Testing (Limited Scope)
- [ ] FDEMTests pass against upgraded FDEMCore
- [ ] Console applications work with upgraded FDEMCore
- [ ] RandomRVEGenerator works with upgraded FDEMCore
- [ ] No regressions in existing functionality

---

### Deferred Success Criteria (Phase 3)

These criteria apply to deferred projects and will be validated in future work:

#### Deferred Framework Targets
- ⏸️ Animation.csproj targets net10.0-windows (SDK-style)
- ⏸️ FDEMWindows.csproj targets net10.0-windows
- ⏸️ PlotFDEM.csproj targets net10.0-windows

#### Deferred Package Updates
- ⏸️ System.Drawing.Common updated to 10.0.5 in Animation.csproj
- ⏸️ SinglePlotZedGraph replaced with compatible alternative

#### Deferred API Compatibility
- ⏸️ 3,758+ Windows Forms API issues resolved (Animation, FDEMWindows, PlotFDEM)
- ⏸️ 454+ System.Drawing API issues resolved
- ⏸️ Legacy Windows Forms controls replaced

---

### Business Success Criteria (Phase 1-2)

#### Functional Parity
- [ ] Upgraded projects work identically to .NET 5 version
- [ ] No loss of functionality in upgraded projects
- [ ] No behavioral changes in upgraded projects
- [ ] UI/UX identical in RandomRVEGenerator

#### Stability
- [ ] No new crashes or exceptions in upgraded projects
- [ ] No memory leaks detected
- [ ] Stable under normal workloads

#### Security
- [ ] No packages with known security vulnerabilities in upgraded projects
- [ ] Deprecated BinaryFormatter removed from FDEMCore

#### Documentation
- [ ] Phase 1-2 changes documented
- [ ] Known issues documented (if any)
- [ ] Testing results documented
- [ ] Deferred work clearly outlined

#### Maintainability
- [ ] Upgraded projects compile cleanly (no warnings)
- [ ] Dependencies up-to-date for upgraded projects
- [ ] Technical debt reduced where applicable

---

### Definition of Done (Phase 1-2)

**Phase 1-2 is complete and ready for commit when:**

1. ✅ **All Phase 1-2 technical success criteria met**
2. ✅ **All Phase 1-2 business success criteria met**
3. ✅ **All tests pass (automated and manual)**
4. ✅ **Performance acceptable for upgraded projects**
5. ✅ **Documentation complete for Phase 1-2**
6. ✅ **Deferred work clearly documented**
7. ✅ **Rollback plan validated**
8. ✅ **Commits tagged appropriately**

**After Phase 1-2 completion:**
- Solution will have 6 projects on .NET 10.0 (4 upgraded + 2 already complete)
- 3 projects remain on older frameworks (to be addressed in Phase 3)
- Clean foundation for future work on deferred projects

---

## Timeline and Effort Estimation

### Time Estimates by Phase - UPDATED

| Phase | Project(s) | Complexity | Estimated Time | Cumulative |
|-------|-----------|------------|----------------|------------|
| **Phase 0** | Pre-Migration Setup | Low | 1-2 hours | 2 hours |
| **Phase 1.1** | FDEMCore | Low | 2-4 hours | 6 hours |
| **Phase 1.2** | FDEMConsole | Low | 1-2 hours | 8 hours |
| **Phase 1.3** | RandomRVEGeneratorConsole | Low | 1-2 hours | 10 hours |
| **Phase 2.1** | RandomRVEGenerator | Medium | 2-4 hours | 14 hours |
| **Phase 1-2 Testing** | Validation | Low | 2-4 hours | 18 hours |
| **TOTAL PHASE 1-2** | **4 Projects** | - | **12-18 hours** | **18 hours** |
| | | | | |
| **DEFERRED WORK** | | | | |
| **Phase 3.1** | Animation (deferred) | High | 8-12 hours | - |
| **Phase 3.2** | FDEMWindows (deferred) | Medium | 4-8 hours | - |
| **Phase 3.3** | PlotFDEM (deferred) | Very High | 16-24 hours | - |
| **ZedGraph Resolution** | Package research/replace | Medium | 4-8 hours | - |
| **Future Testing** | Deferred projects | Medium | 8-12 hours | - |
| **TOTAL DEFERRED** | **3 Projects** | - | **40-64 hours** | - |

### Total Estimated Time - UPDATED FOR PHASE 1-2 ONLY

**Phase 1-2 Only (Immediate Scope):**

**Optimistic:** 8 hours (no issues, fast progress)  
**Realistic:** 12 hours (typical issues, steady progress)  
**Pessimistic:** 16 hours (minor issues, careful validation)

**Deferred Work (Phase 3):**
- Animation SDK conversion: 8-12 hours
- SinglePlotZedGraph resolution: 4-8 hours
- FDEMWindows migration: 4-8 hours
- PlotFDEM migration: 16-24 hours
- **Total deferred:** 32-52 hours (to be scheduled separately)

### Timeline by Calendar - UPDATED

**Phase 1-2 Only (Assuming 1 developer working full-time - 8 hours/day):**

| Scenario | Developer Days | Calendar Days |
|----------|----------------|---------------|
| Optimistic | 1 day | 1 day |
| Realistic | 1.5 days | 2 days |
| Pessimistic | 2 days | 2-3 days |

**Phase 1-2 Only (Assuming 1 developer working part-time - 4 hours/day):**

| Scenario | Developer Days | Calendar Days |
|----------|----------------|----------------|
| Optimistic | 2 days | 2 days |
| Realistic | 3 days | 3-4 days |
| Pessimistic | 4 days | 4-5 days |

### Recommended Schedule - UPDATED

**Week 1 (Phase 1-2 Focus):**
- Day 1 Morning: Phase 0 setup and preparation
- Day 1 Afternoon: Phase 1.1 - FDEMCore migration
- Day 2 Morning: Phase 1.2 & 1.3 - Console projects
- Day 2 Afternoon: Phase 2.1 - RandomRVEGenerator (start)
- Day 3: Phase 2.1 complete + testing and validation

**Future Work (Scheduled Separately):**
- Week 2-3: Address SinglePlotZedGraph (research, decide, implement)
- Week 3-4: Animation SDK conversion
- Week 4-5: FDEMWindows and PlotFDEM migration
- Week 5-6: Full solution testing and integration

---

## Pre-Migration Setup (Phase 0)

### Prerequisites Checklist

Before starting Phase 1, ensure the following are in place:

#### Development Environment
- [ ] Visual Studio 2022 (version 17.12 or later) installed
- [ ] .NET 10.0 SDK installed and verified
  ```bash
  dotnet --list-sdks
  # Verify net10.0 SDK appears in list
  ```
- [ ] Visual Studio workloads installed:
  - [ ] .NET desktop development
  - [ ] Windows Forms
- [ ] Git installed and configured
- [ ] Solution builds successfully on current frameworks

#### Backup and Version Control
- [ ] All changes committed to `main` branch
- [ ] No pending uncommitted changes
- [ ] Branch `upgrade-to-NET10` created
  ```bash
  git checkout -b upgrade-to-NET10
  ```
- [ ] Full backup of repository created (optional but recommended)

#### Documentation Preparation
- [ ] Assessment.md reviewed and understood
- [ ] Plan.md (this document) reviewed
- [ ] Migration notes document created

#### Testing Infrastructure
- [ ] Test data prepared
- [ ] Baseline test results captured (run tests on .NET 5)
  ```bash
  dotnet test FDEMTests\FDEMTests.csproj --logger "trx;LogFileName=baseline-results.trx"
  ```
- [ ] Baseline performance benchmarks captured (if applicable)

#### Critical Decisions Made
- [ ] **SinglePlotZedGraph resolution approach decided** ⚠️ CRITICAL
- [ ] Stakeholders informed of migration start
- [ ] Rollback procedure understood

#### Tools and Resources
- [ ] .NET Upgrade Assistant installed (optional but recommended)
  ```bash
  dotnet tool install -g upgrade-assistant
  ```
- [ ] .NET breaking changes documentation bookmarked
  - https://learn.microsoft.com/en-us/dotnet/core/compatibility/
- [ ] Windows Forms migration guide bookmarked
- [ ] This plan.md accessible for reference

---

## Post-Migration Tasks

### After All Phases Complete

#### 1. Final Validation
- [ ] Run complete test suite one more time
- [ ] Perform full regression testing
- [ ] Execute all end-to-end scenarios
- [ ] Performance benchmarking comparison
- [ ] Memory profiling

#### 2. Code Review
- [ ] Review all changed files
- [ ] Verify no debugging code left in
- [ ] Check for TODO/HACK/FIXME comments
- [ ] Ensure consistent formatting
- [ ] Review for code quality

#### 3. Documentation Updates
- [ ] Update README.md with new framework requirements
- [ ] Update developer setup documentation
- [ ] Document any breaking changes
- [ ] Update system requirements documentation
- [ ] Add migration notes to changelog

#### 4. CI/CD Pipeline Updates
- [ ] Update build pipeline to target .NET 10.0
- [ ] Update deployment scripts
- [ ] Update Docker files (if applicable)
- [ ] Update GitHub Actions / Azure DevOps (if applicable)
- [ ] Test CI/CD pipeline with new version

#### 5. Dependencies Cleanup
- [ ] Remove any temporary workarounds
- [ ] Clean up unused NuGet packages
- [ ] Verify all package versions are latest compatible
- [ ] Run NuGet package cleanup

#### 6. Quality Gates
- [ ] Run static code analysis
- [ ] Run security scanning
- [ ] Check for code coverage (should not decrease)
- [ ] Performance profiling

#### 7. Stakeholder Communication
- [ ] Notify stakeholders of completion
- [ ] Schedule demo of migrated solution
- [ ] Provide migration summary report
- [ ] Share lessons learned

#### 8. Merge Strategy
- [ ] Final review of all changes in `upgrade-to-NET10` branch
- [ ] Create pull request to `main`
- [ ] Peer review
- [ ] Approval from stakeholders
- [ ] Merge to `main`
- [ ] Tag release: `v10.0-migration-complete`

---

## Lessons Learned and Best Practices

### For Future .NET Upgrades

#### What Worked Well
- Incremental phased approach (reduces risk)
- Dependency-based migration order (prevents breaking references)
- Comprehensive assessment before planning (identifies all issues upfront)
- Regular testing at phase boundaries (catches issues early)

#### Potential Improvements
- Earlier investigation of incompatible packages (SinglePlotZedGraph should be researched before migration starts)
- Automated testing coverage (more automated tests = faster validation)
- Performance baselines (establish before migration for comparison)

#### Recommendations for Next Time
1. **Automate Where Possible:**
   - Use `upgrade-assistant` for SDK conversions
   - Leverage designer auto-regeneration for WinForms
   - Script repetitive tasks

2. **Test Early and Often:**
   - Test after each project
   - Don't batch multiple projects without intermediate testing
   - Commit working states frequently

3. **Package Research:**
   - Investigate all packages before starting
   - Have replacement plans for incompatible packages
   - Don't discover blockers mid-migration

4. **Communication:**
   - Keep stakeholders informed
   - Document progress daily
   - Report blockers immediately

5. **Risk Mitigation:**
   - Always have rollback plan
   - Use feature branches
   - Tag successful phases
   - Keep backups

---

## Appendix A: Command Reference

### Common Commands During Migration

#### Building
```bash
# Build specific project
dotnet build <ProjectPath> --configuration Release

# Build entire solution
dotnet build FiberDEM.sln --configuration Release

# Clean build
dotnet clean FiberDEM.sln
dotnet build FiberDEM.sln --configuration Release
```

#### Testing
```bash
# Run all tests
dotnet test FDEMTests\FDEMTests.csproj

# Run tests with detailed output
dotnet test FDEMTests\FDEMTests.csproj --logger "console;verbosity=detailed"

# Run tests and save results
dotnet test FDEMTests\FDEMTests.csproj --logger "trx;LogFileName=test-results.trx"
```

#### Package Management
```bash
# List packages in a project
dotnet list <ProjectPath> package

# Update a package
dotnet add <ProjectPath> package <PackageName> --version <Version>

# Remove a package
dotnet remove <ProjectPath> package <PackageName>
```

#### Version Checking
```bash
# Check installed SDKs
dotnet --list-sdks

# Check runtime versions
dotnet --list-runtimes

# Check project framework
dotnet list <ProjectPath> reference
```

#### Git Commands
```bash
# Create and switch to upgrade branch
git checkout -b upgrade-to-NET10

# Commit changes
git add .
git commit -m "Phase 1: Migrate FDEMCore to .NET 10.0"

# Tag phase completion
git tag phase-1-complete

# Push branch
git push origin upgrade-to-NET10

# Revert last commit (if needed)
git revert HEAD
```

---

## Appendix B: Package Resolution for SinglePlotZedGraph

### Investigation Steps

1. **Check NuGet.org for Updates**
   ```
   https://www.nuget.org/packages/SinglePlotZedGraph
   ```
   - Check for newer versions
   - Review supported frameworks
   - Check last update date

2. **Search for Source Repository**
   - GitHub search for "SinglePlotZedGraph"
   - Check package metadata for project URL
   - Look for forks with .NET Core support

3. **Evaluate Alternatives**

   **Option A: ScottPlot** (Recommended)
   - Modern, actively maintained
   - .NET 10.0 compatible
   - Good performance
   - Similar API to ZedGraph
   - https://scottplot.net/
   
   **Option B: OxyPlot**
   - Mature charting library
   - Cross-platform
   - .NET 10.0 compatible
   - https://oxyplot.github.io/
   
   **Option C: LiveCharts2**
   - Modern, reactive charts
   - .NET 10.0 compatible
   - https://livecharts.dev/

4. **Migration Effort Estimation**
   - Review current usage in code
   - Identify API mapping between libraries
   - Estimate refactoring effort

### Decision to be Made Before Phase 2.3

**Decision Point:** Before starting FDEMWindows and PlotFDEM migration

**Document Decision Here:**
```
Decision: [To be filled in during Phase 0]
Approach: [Package update / Library replacement / Custom implementation]
Rationale: [Why this approach was chosen]
Estimated Effort: [Hours]
```

---

## Appendix C: Reference Links

### Official Microsoft Documentation
- [.NET 10.0 Release Notes](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-10)
- [Breaking Changes in .NET 10.0](https://learn.microsoft.com/en-us/dotnet/core/compatibility/10.0)
- [Migrate from .NET Framework to .NET](https://learn.microsoft.com/en-us/dotnet/core/porting/)
- [Windows Forms Migration Guide](https://learn.microsoft.com/en-us/dotnet/desktop/winforms/migration/)
- [SDK-style Project Format](https://learn.microsoft.com/en-us/dotnet/core/project-sdk/overview)

### Tools
- [.NET Upgrade Assistant](https://dotnet.microsoft.com/en-us/platform/upgrade-assistant)
- [.NET Portability Analyzer](https://learn.microsoft.com/en-us/dotnet/standard/analyzers/portability-analyzer)

### Community Resources
- [Stack Overflow - .NET 10.0 Migration](https://stackoverflow.com/questions/tagged/.net-10.0)
- [GitHub - .NET Upgrade Assistant Issues](https://github.com/dotnet/upgrade-assistant/issues)

---

## Contact and Support

### Escalation Path

**For Technical Issues:**
- Review assessment.md for detailed issue information
- Consult .NET breaking changes documentation
- Search Stack Overflow for similar issues
- Open issue on relevant GitHub repositories

**For Blocker Issues:**
- Document issue thoroughly
- Identify rollback point
- Escalate to team lead / architect
- Consider community support channels

---

## Approval and Sign-Off

### Plan Review

**Plan Created By:** GitHub Copilot Modernization Agent  
**Plan Creation Date:** January 2025  
**Plan Version:** 1.0

### Approval Checklist

Before proceeding with migration, the following stakeholders should review and approve this plan:

- [ ] **Tech Lead / Architect** - Review technical approach
- [ ] **Project Manager** - Review timeline and resource allocation
- [ ] **QA Lead** - Review testing strategy
- [ ] **Product Owner** - Review risk and business impact
- [ ] **Development Team** - Review feasibility and effort estimates

### Sign-Off

| Role | Name | Date | Signature |
|------|------|------|-----------|
| Tech Lead / Architect | ______________ | ______ | ______________ |
| Development Lead | ______________ | ______ | ______________ |
| QA Lead | ______________ | ______ | ______________ |
| Product Owner | ______________ | ______ | ______________ |

---

**Note:** This plan is a living document and may be updated as migration progresses and new information becomes available. All significant changes should be documented with date and rationale.

---

## Migration Status Tracking

*Use this section to track progress during migration*

### Phase Completion Status

| Phase | Status | Start Date | Completion Date | Notes |
|-------|--------|------------|-----------------|-------|
| Phase 0: Setup | ⬜ Not Started | - | - | |
| Phase 1.1: FDEMCore | ⬜ Not Started | - | - | |
| Phase 1.2: FDEMConsole | ⬜ Not Started | - | - | |
| Phase 1.3: RandomRVEGeneratorConsole | ⬜ Not Started | - | - | |
| Phase 2.1: RandomRVEGenerator | ⬜ Not Started | - | - | |
| Phase 1-2 Testing | ⬜ Not Started | - | - | |
| **DEFERRED PHASES** | | | | |
| Phase 3.1: Animation | ⏸️ Deferred | - | - | Requires SDK conversion |
| Phase 3.2: FDEMWindows | ⏸️ Deferred | - | - | Blocked by SinglePlotZedGraph |
| Phase 3.3: PlotFDEM | ⏸️ Deferred | - | - | Blocked by Animation + SinglePlotZedGraph |
| ZedGraph Resolution | ⏸️ Deferred | - | - | Research ScottPlot/OxyPlot replacement |

**Legend:**
- ⬜ Not Started
- 🔄 In Progress
- ✅ Complete
- ⚠️ Blocked
- ❌ Failed/Rolled Back

### Known Issues During Migration

*Document issues as they arise*

| Issue # | Description | Affected Phase | Status | Resolution |
|---------|-------------|----------------|--------|------------|
| | | | | |

---

**END OF PLAN**
