# 🐽 Pigsty - Android Storage Manager for Windows

[![Freeware](https://img.shields.io/badge/License-MIT%20Freeware-blue.svg)](LICENSE)
[![Platform](https://img.shields.io/badge/Platform-Windows%2010%20%7C%2011-0078D6.svg)]()
[![No Developer Options](https://img.shields.io/badge/Developer%20Options-Not%20Required-success.svg)]()
[![Branch: dev](https://img.shields.io/badge/Branch-dev%20%2F%20main-gray.svg)]()

> **Clean, modern Windows freeware to organize, analyze, export, import, and free up Android device storage — 100% without Developer Options or USB Debugging.**

---

## 📌 Why Pigsty?

Managing files on an Android phone from a Windows PC has always been painful:

| The Problem with Windows Explorer | The Problem with ADB | **The Pigsty Solution** |
| :--- | :--- | :--- |
| ❌ Extremely slow search & browsing | ❌ Requires enabling "Developer Options" | **Zero developer options required** |
| ❌ No storage categorization | ❌ Requires typing 7 times on build number | **Instant categorization** (Photos, Videos, Music, Docs, APKs, Downloads) |
| ❌ Can't find large files eating space | ❌ Requires RSA prompt approval on phone | **Large Files Finder** (>50MB / >100MB) |
| ❌ Cluttered folder structure (`DCIM`, `WhatsApp`, etc.) | ❌ Requires installing command-line tools | **1-Click Export to PC & Import from PC** |
| ❌ Often freezes or hangs during file transfers | ❌ Intimidating for normal users | **Modern, calm, non-flashy UI** |

Android devices connected via standard USB use **MTP (Media Transfer Protocol)** through **Windows Portable Devices (WPD)**. **Pigsty** leverages this native protocol directly, giving you complete file management capabilities without needing developer options, adb drivers, or rooting.

---

## ✨ Key Features

### 1. 🔌 Zero Developer Options / Zero Configuration
* Simply plug your Android phone or tablet into your Windows PC with a USB cable.
* Unlock your screen and choose **"File Transfer / MTP"** in the USB notification.
* Pigsty immediately detects the device and mounts its internal storage and SD card.

### 2. 📊 Smart Storage Categorization
Automatically indexes and groups your phone's storage into clean, dedicated categories:
* 📸 **Photos & Images**: Camera captures, screenshots, WhatsApp images, wallpapers (`.jpg`, `.png`, `.heic`, `.webp`, `.dng`, `.raw`, `.svg`).
* 🎬 **Videos & Movies**: Recorded clips, downloaded videos, screen recordings (`.mp4`, `.mkv`, `.mov`, `.3gp`, `.webm`, `.avi`).
* 🎵 **Music & Audio**: Songs, voice memos, podcasts, ringtones, notifications (`.mp3`, `.m4a`, `.wav`, `.flac`, `.aac`, `.ogg`, `.opus`).
* 📄 **Documents & PDFs**: Office documents, PDFs, eBooks, spreadsheets (`.pdf`, `.docx`, `.xlsx`, `.pptx`, `.txt`, `.epub`).
* 📦 **APKs & Installers**: Android package files cluttering your downloads (`.apk`, `.xapk`, `.apks`, `.aab`).
* 📥 **Downloads**: Consolidated view of all items in your phone's `Download` folder.
* 🗃️ **Archives**: Compressed ZIP, RAR, 7Z, and TAR archives (`.zip`, `.rar`, `.7z`, `.tar`, `.gz`).
* ⚠️ **Large Files (> 50 MB)**: Instantly locate the biggest space-hogging files on your device to clean storage fast.
* 📁 **All Files**: Comprehensive file explorer across the entire phone storage.

### 3. 💾 Full File Management
* **⬇️ Export to PC**: Select one or multiple files (or entire categories) and export them directly to any folder on your Windows PC with real-time transfer progress.
* **⬆️ Import to Android**: Select files from your PC (or drag and drop) and import them directly to designated folders on your phone (`Download`, `DCIM`, `Pictures`, `Music`, `Documents`).
* **🗑️ Delete Selected**: Safely remove files directly from the Android device with confirmation dialogs and instant space recovery calculations.
* **🔍 Instant Search & Filter**: Real-time search by file name, extension, or directory path.
* **⚡ Sorting**: Sort by Name (A-Z), Size (Largest / Smallest First), or Date Modified (Newest / Oldest First).

### 4. 🛡️ Safe, Secure & Resilient Architecture
* **Accidental Data Loss Safeguards**: Strict validation prevents deletion of protected Android system folders (`\Android`, `\Android\data`, root storage volumes). Only individual user files can be deleted with explicit confirmation.
* **Path Traversal & Sanitization**: Automatically sanitizes filenames containing characters valid on Linux/Android but illegal on Windows (`:`, `*`, `?`, `"`, `<`, `>`, `|`), preventing path crashes and corrupted writes.
* **Duplicate Protection**: Avoids silent overwrites during export by automatically appending safe numbering `(1)`, `(2)`.
* **MTP Driver Buffer Protection**: Throttles operations and handles Windows Portable Devices buffers to avoid 0x80004005 USB driver lockups.
* **Graceful Disconnection Recovery**: Unplugging the USB cable during an operation is caught gracefully without crashing the app.
* **Least-Privilege Execution**: Uses standard user permissions (`asInvoker`) — no administrator privileges required.
* **Drag & Drop Support**: Drag files directly from your PC into Pigsty to import them to your Android device instantly.
* **High-DPI PerMonitorV2 Aware**: Crisp rendering on 1080p, 1440p, and 4K displays.

### 5. 🎨 Proper UI Without Flashy Colors
* Built with a restrained, distraction-free **Modern Slate aesthetic** (`#14161C` background, `#1D2028` panels, `#3B82F6` calm slate blue accent).
* High-contrast, clean Segoe UI typography with clear hierarchy.
* Proportional multi-segment storage bar using muted pastel indicators.
* Zero eye-straining neon colors, zero flashy animations, zero advertisements.

---

## 🖥️ UI Layout Overview

```
+---------------------------------------------------------------------------------------------+
| 🐽 Pigsty                     [Google Pixel 8 Pro v] [● Connected] [Scan]         [About]   |
+---------------------------------------------------------------------------------------------+
| Google Pixel 8 Pro • Internal shared storage                 84.2 GB used of 128.0 GB       |
| [■ Photos 18.4GB][■ Videos 32.1GB][■ Audio 6.2GB][■ Docs 1.8GB][■ APKs 3.5GB][■ Free 43.8GB] |
+-------------------+-------------------------------------------------------------------------+
| CATEGORIES        | Photos • 120 items (4.2 GB)                             [Search...    ] |
|                   +-------------------------------------------------------------------------+
| Overview          | [Export] [Import] [Delete] | [Select All] [Clear]           [Sort: Name v] |
| Photos            +-------------------------------------------------------------------------+
| Videos            | [✓] | Type | Name                      | Category | Size    | Date          |
| Audio             |-----+------+---------------------------+----------+---------+---------------|
| Documents         | [x] | 📷   | PXL_20261024_124500.jpg   | Photos   | 4.3 MB  | 2026-10-24    |
| APKs              | [x] | 📷   | PXL_20261024_141022.jpg   | Photos   | 5.1 MB  | 2026-10-24    |
| Downloads         | [ ] | 📷   | Screenshot_20261025.png   | Photos   | 1.8 MB  | 2026-10-25    |
| Archives          | [ ] | 📷   | Sunset_Coast.dng          | Photos   | 45.0 MB | 2026-10-20    |
| Large Files       |                                                                         |
| All Files         |                                                                         |
+-------------------+-------------------------------------------------------------------------+
| Ready • 120 items indexed (4.2 GB)           Selected: 2 items (9.4 MB)        Pigsty v1.0.1  |
+---------------------------------------------------------------------------------------------+
```

---

## 🚀 Getting Started

### Prerequisites
* Windows 10 or Windows 11 (64-bit or 32-bit).
* Standard Android USB cable (USB-C or Micro-USB).
* *.NET Framework 4.7.2 or higher* (pre-installed on almost all Windows 10/11 machines).
* **NO Android SDK, NO ADB, and NO Developer Options required!**

### Running the Single Portable Executable (No Install Required)
Pigsty is a 100% portable, standalone freeware app. Simply download and double-click:
```
release\Pigsty.exe
```
No installation, no registry pollution, no admin rights, and no dependencies needed. You can run it directly from your Downloads folder, Desktop, or a USB thumb drive.

### Optional 1-Click Installation (If You Prefer an Installed App)
If you prefer a permanent Desktop shortcut, Start Menu entry, and Windows Settings integration:
* Run `install.bat` — copies Pigsty to your local user app directory and creates shortcuts.
* Run `uninstall.bat` — cleanly uninstalls shortcuts and local files at any time.

---

## 🛠️ Building from Source

Pigsty requires zero external NuGet packages or third-party dependencies. It builds using the native Microsoft Build Engine included with the Windows .NET Framework.

### Option 1: Using PowerShell
```powershell
.\build.ps1
```

### Option 2: Using Command Prompt / Batch
```cmd
build.bat
```

### Output
The compiled standalone executable and verified checksum are generated at:
```
release\
├── Pigsty.exe          # Single portable Windows freeware binary (~120 KB)
└── SHA256SUMS.txt      # Cryptographic SHA-256 integrity hash
```

---

## 🌿 Git Branch Workflow

This repository follows a clean, professional Git branching strategy:

* **`main`**: Production-ready, stable releases. Contains verified, release-grade freeware code.
* **`dev`**: Active development branch. New features, UI improvements, and optimizations are developed here before merging into `main`.

---

## 📂 Project Structure

```
Pigsty/
├── .gitignore               # Git ignore rules for build artifacts
├── LICENSE                  # MIT Open Source Freeware License
├── README.md                # Comprehensive documentation
├── build.bat                # 1-Click batch build script
├── build.ps1                # PowerShell build and packaging script
├── install.bat              # Optional 1-click desktop & start menu installer
├── uninstall.bat            # Clean 1-click uninstaller
├── release/                 # Standalone release output
│   ├── Pigsty.exe           # Single portable Windows freeware binary (~120 KB)
│   └── SHA256SUMS.txt       # Cryptographic SHA-256 integrity checksum
└── src/                     # C# / WPF Source Code
    ├── Pigsty.csproj        # MSBuild project file
    ├── app.manifest         # High-DPI & Windows 10/11 security manifest
    ├── App.xaml             # Application definitions
    ├── App.xaml.cs          # Application entry point
    ├── MainWindow.xaml      # Main user interface
    ├── MainWindow.xaml.cs   # Core UI, drag-drop, & file operations
    ├── Models/
    │   ├── AndroidDevice.cs    # Device and volume model
    │   ├── AndroidFileItem.cs  # File item and metadata model
    │   ├── StorageCategory.cs  # Category definitions and stats
    │   └── StorageStats.cs     # Aggregate storage calculations
    ├── Services/
    │   ├── IMtpDeviceService.cs     # Device management interface
    │   └── ShellMtpDeviceService.cs # Native Windows WPD / MTP service
    ├── Helpers/
    │   ├── FileFormatHelper.cs # Extensions & category detection
    │   ├── PathSanitizer.cs    # Path traversal & deletion safety validation
    │   ├── SizeFormatter.cs    # Human-readable byte formatting
    │   └── RelayCommand.cs     # MVVM command helper
    ├── UI/
    │   ├── Styles.xaml         # Modern slate dark theme resources
    │   ├── ConfirmDialog.xaml  # Deletion confirmation modal
    │   ├── ProgressDialog.xaml # File transfer progress modal
    │   └── ImportDialog.xaml   # Destination folder selector modal
    └── Properties/
        └── AssemblyInfo.cs     # Assembly metadata
```

---

## 📄 License

This project is licensed under the **MIT License** — a free and open-source software license. You are free to use, modify, distribute, and integrate it without restrictions. See the [LICENSE](LICENSE) file for details.
