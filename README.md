# MailCraft AI

**MailCraft AI** is an intelligent Windows desktop assistant built with **C# / .NET 8 and WPF** that connects directly to **classic Microsoft Outlook desktop (Outlook Classic)** via COM interop to generate executive-grade, professional email replies using **Anthropic Claude**, **OpenAI**, or **Google Gemini**.

---

## Key Features

- **Classic Outlook COM Integration**:
  - One-click **"Pull from Outlook"** reads the email currently selected in Outlook or open in its own window (sender, recipients, subject, date, body, conversation history, attachment names).
  - One-click **"Create Draft in Outlook"** (Reply or Reply All) creates a real Outlook draft, preserves the user's default Outlook signature, places the AI reply above the quoted thread, and displays the draft window.
  - **Guaranteed Safety**: The application **never** sends an email automatically. All sends must be manually reviewed and sent by the user inside Outlook.
  - Standalone drafting: "Create New Email" for pasted or document-imported context.

- **Multi-Provider AI Layer**:
  - **Anthropic Claude**: Claude 3.7 Sonnet (Latest), Claude 3.5 Sonnet, Claude 3.5 Haiku, Claude 3 Opus.
  - **OpenAI**: GPT-4o, GPT-4o-mini, o3-mini, GPT-4 Turbo (with custom endpoint support for corporate Azure OpenAI/proxies).
  - **Google Gemini**: Gemini 2.5 Flash (Recommended), Gemini 2.5 Pro, Gemini 3.8 Flash, Gemini 3.8 Flash-Lite.
  - Full **live token streaming (SSE)**: Text streams in real time as the AI generates.

- **3 Flexible Context Input Methods**:
  1. **Outlook Current Email**: Direct COM connection.
  2. **Paste Text**: Multi-line paste area for copying email threads or inquiry messages.
  3. **File Import / Drag & Drop**: Automatic text extraction from `.msg` (MsgReader), `.eml` (MimeKit), `.pdf` (PdfPig), `.docx` (OpenXML SDK), and `.txt`/`.md`.

- **Prompt Engineering & Personas**:
  - Grounded system prompt: matches sender's formality, answers every question, never hallucinates dates/prices (uses `[placeholders]`).
  - **Business Profile Injection**: Saves your full name, job title, company name, scope, standard phrases, and default sign-off in Settings to ensure every reply reflects your brand.
  - **Personas**: Switch between *General Professional*, *Customer Support*, *Sales & Quotation*, *Vendor Follow-up*, and *Executive*.

- **Quick Actions Toolbar**:
  - Refine any draft with one click: **Shorten**, **Make more formal**, **Make friendlier**, **Fix grammar**, or **Translate**.

- **Security & Privacy**:
  - API keys are encrypted locally using **Windows DPAPI** (`DataProtectionScope.CurrentUser`). Never hardcoded or shared.
  - **Bi-directional PII Masking Engine**: Automatically masks emails (`{{EMAIL_1}}`), phone numbers (`{{PHONE_1}}`), credit cards (`{{CARD_1}}`), and IBANs (`{{IBAN_1}}`) before sending to AI endpoints and restores them upon return.
  - **Local SQLite History**: All past generations are saved locally (`%APPDATA%\MailCraftAI\history.db`) with full-text search, favorites, and "Load into Editor" capability.
  - **Intelligent Context Trimming**: Automatically trims long repetitive email threads when exceeding thresholds to save tokens and maintain focus on the latest message.

- **Windows Desktop Integration**:
  - **Global Hotkey (`Ctrl+Alt+R`)**: Pressing `Ctrl+Alt+R` anywhere in Windows brings MailCraft AI to the front, pulls the currently active email in Outlook, and drafts a reply.
  - **Light & Dark Theme**: Toggle between high-contrast dark mode and clean daylight mode.

---

## System Requirements

- **Operating System**: Windows 10 (version 1809 or higher) or Windows 11 (64-bit recommended)
- **Runtime**: [.NET 8 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/8.0)
- **Email Client**: **Classic Microsoft Outlook desktop** (2016, 2019, 2021, or Microsoft 365 desktop).
  > *Note: The "New Outlook for Windows" web-based client does not support COM interop automation. MailCraft AI targets the traditional classic Outlook desktop client.*

---

## Getting Started

### 1. Build and Run from Source

```powershell
# Clone or navigate to the repository
cd e:\automatedemails

# Restore NuGet dependencies
dotnet restore

# Run automated tests
dotnet test

# Launch the desktop application
dotnet run --project src/MailCraftAI/MailCraftAI.csproj
```

### 2. Publish a Release Build

```powershell
dotnet publish src/MailCraftAI/MailCraftAI.csproj -c Release
```
Output files will be located in:
`src\MailCraftAI\bin\Release\net8.0-windows\publish\`

### 3. Build Windows Installer (Inno Setup)

An Inno Setup compilation script is provided in `installer/MailCraftAI.iss`:
1. Download and install [Inno Setup 6](https://jrsoftware.org/isdl.php).
2. Right-click `installer/MailCraftAI.iss` and choose **Compile**, or run:
   ```powershell
   iscc installer/MailCraftAI.iss
   ```
3. The setup package will be generated at `publish\installer\MailCraftAI-Setup.exe`.

---

## Configuring AI Providers & API Keys

Open the app and click the **Settings (gear icon)** in the top right header:

1. **Anthropic Claude**:
   - Get an API key at [console.anthropic.com](https://console.anthropic.com).
   - Enter your key (`sk-ant-...`) and click **Test & Validate API Key**.
2. **OpenAI**:
   - Get an API key at [platform.openai.com](https://platform.openai.com).
   - Enter your key (`sk-...`). (Optional: provide custom endpoint for Azure OpenAI).
3. **Google Gemini**:
   - Get an API key at [aistudio.google.com](https://aistudio.google.com).
   - Enter your key (`AIzaSy...`).

---

## Outlook COM Troubleshooting Guide

### 1. "Outlook is running, but access was denied" (Elevation Mismatch)
- **Cause**: Windows User Account Control (UAC) integrity level mismatch. If Outlook was started by another application running "As Administrator" while MailCraft AI is running as standard user (or vice versa), Windows COM blocks inter-process communication with error code `0x80080005` or `0x800401E3`.
- **Solution**: Ensure both Outlook and MailCraft AI are started normally **without** "Run as Administrator".

### 2. "Classic Outlook is not registered on this PC"
- **Cause**: The PC is running the "New Outlook for Windows" preview toggle or only web Outlook is installed.
- **Solution**: Switch the toggle in Outlook from "New Outlook" back to Classic Outlook, or launch Classic Outlook from the Start Menu (`outlook.exe`).

### 3. Outlook Security Warning Dialog
- **Cause**: Older Antivirus definitions or strict Exchange Group Policies prompting: *"A program is trying to access email address information..."*.
- **Solution**: Ensure your Windows Defender / Antivirus status is active and up to date. In Outlook Trust Center (`File -> Options -> Trust Center -> Trust Center Settings -> Programmatic Access`), verify that programmatic access is set to *"Warn me about suspicious activity when my antivirus software is inactive or out of date"* (recommended).

---

## Project Architecture

```
e:\automatedemails\
├── MailCraftAI.sln
├── src\
│   └── MailCraftAI\
│       ├── App.xaml / App.xaml.cs                  # DI container & theme management
│       ├── MainWindow.xaml / MainWindow.xaml.cs    # 2-panel modern workspace
│       ├── Models\                                 # Data contracts & DTOs
│       ├── Services\
│       │   ├── Outlook\                            # Classic Outlook COM interop
│       │   ├── AI\                                 # Claude, OpenAI, Gemini SSE providers
│       │   ├── Security\                           # Windows DPAPI & PII masking
│       │   ├── Context\                            # PDF, DOCX, MSG, EML & trimmer
│       │   ├── Prompt\                             # Executive prompt engineering
│       │   ├── History\                            # Embedded SQLite database
│       │   ├── Settings\                           # Local settings manager
│       │   └── SystemIntegration\                  # Win32 global hotkeys
│       ├── ViewModels\                             # CommunityToolkit.Mvvm ViewModels
│       ├── Styles\                                 # Modern Dark/Light themes, controls & icons
│       └── Converters\                             # WPF value converters
├── tests\
│   └── MailCraftAI.Tests\                          # 15 comprehensive unit tests
├── installer\
│   └── MailCraftAI.iss                             # Inno Setup installation script
└── README.md
```
