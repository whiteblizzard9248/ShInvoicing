# AI Agent Instructions: .NET 10 Avalonia Invoice Manager

## 1. Project Overview & Requirements

- **Target Framework:** .NET 10.0 (LTS).
- **Platform:** Linux Development (Cross-platform distribution for Windows/Linux).
- **Primary Goal:** Read `.xlsx` files, display invoice details, and generate "designable" PDFs.
- **Core Features:**
  - **Excel Integration:** Parse "Invoices" and "Invoice Items" sheets (joining via `Invoice No`).
  - **Database:** Use SQLite to track generated invoices and prevent duplicates.
  - **PDF Engine:** Use QuestPDF for code-based, customizable layouts.
  - **Distribution:** Support per-customer branding (Logo, Primary Colors, Address) via a local JSON config.

## 2. Technical Stack & Architecture

- **UI Framework:** Avalonia UI (MVVM Template).
- **MVVM Toolkit:** `CommunityToolkit.Mvvm` (Recommended for .NET 10).
- **Excel Library:** `ClosedXML`.
- **ORM:** `Microsoft.EntityFrameworkCore.Sqlite`.
- **PDF Generation:** `QuestPDF`.

### Project Structure (Standard)

1.  **Models:** POCO classes for `Invoice`, `InvoiceItem`, and `VendorSettings`.
2.  **ViewModels:** Logic for file selection, invoice filtering, and command execution.
3.  **Views:** XAML files for the UI (using `DataGrid` for items and `ComboBox` for selection).
4.  **Services:**
    - `ExcelService`: Logic for reading and joining XLSX data.
    - `InvoiceDbService`: EF Core context and CRUD logic for SQLite.
    - `PdfGenerationService`: QuestPDF templates using branding settings.

## 3. Best Practices & Design Principles

- **Zero-Hardcoding:** All paths (Home Directory) and branding (Colors/Logo) must be resolved at runtime using `Environment.SpecialFolder` and a configuration JSON.
- **Async-First:** All File I/O (Excel/PDF) and DB operations must be `async` to keep the UI responsive.
- **Data Integrity:** \* Check the SQLite DB for an `Invoice No` before generating.
  - If a duplicate is found, prompt the user to "Open Existing" or "Overwrite."
- **Styling:** Use Avalonia `Styles` in `App.axaml` for global UI consistency. Use QuestPDF `TextStyle` for document design.
- **Linux Compatibility:** Avoid any `System.Drawing.Common` or Win32 API calls. Use `SkiaSharp`-based libraries (like QuestPDF) for cross-platform rendering.

## 4. Implementation Guidelines for Agent

### Excel Data Handling

- Handle the specific format provided: "Invoices" sheet contains header info; "Invoice Items" contains line items.
- Filter out empty rows (common in exported Excel files).

### SQLite & EF Core Setup

- Ensure the database file is stored in `AppDomain.CurrentDomain.BaseDirectory` or a subfolder in the user's `.config` (Linux) or `AppData` (Windows).
- Use migrations to keep the schema versioned.

### QuestPDF Customization

- Implement a `DocumentSettings` model that is passed to the PDF generator.
- The template should dynamically adjust colors and headers based on this model.

## 5. Deployment & Distribution

- The agent should provide `dotnet publish` commands for `win-x64` (Self-contained) so customers do not need a pre-installed runtime.
- Ensure `appsettings.json` or `branding.json` is marked as "Copy to Output Directory" so it can be customized per customer.

## 6. Implemented Features (done)

- Excel importer now loads `Invoices` and `Invoice Items` and maps vendor header fields from `Invoices` sheet.
- Invoice records are grouped by `Invoice No` with line item aggregation.
- `VendorSettings` now includes:
  - `VendorName`, `VendorAddress`, `GSTIN`, `PANNo`, `BankAccountNo`, `IFSC`, `LogoPath`, `Address` (output path).
- Vendor editable form in `MainWindow.axaml` with two-way binding and save button updates `branding.json`.
- Added file/folder pickers:
  - `Browse Logo` sets logo file path.
  - `Select Folder` sets PDF output directory for generation (customer-chosen "Address").
- `GeneratePdfAsync` uses selected output folder and falls back to Desktop if empty.
- PDF generator uses `LogoPath` if valid and includes output folder info in the header.
- `SelectedInvoice` change triggers `InvoiceItems` refresh in DataGrid (`SelectedInvoice.Items` -> `InvoiceItems`).

### 7. Suggested next improvements

- Add validation and error messages for incorrect path/invalid logo file formats.
- Add a per-invoice `VendorSettings` profile selector to support multiple clients (persist mapping per vendor).
- Implement RLS and permissions for secure multi-user operation.
- Add preferences for tax rate presets and rounding rules in PDF detail lines.
- Add unit tests for Excel parsing + PDF generation output structure.

---

### Sample Code Patterns for Agent

**Prompting the agent for specific tasks:**

- _"Scaffold the `Invoice` and `InvoiceItem` models based on the CSV headers provided."_
- _"Create an `ExcelService` using ClosedXML that joins the two sheets into a single `Invoice` object."_
- _"Write a QuestPDF template that takes a `BrandingConfig` object to set the primary theme color."_
- _"Implement a duplicate check in the `GenerateCommand` using EF Core."_
- _"Design an Avalonia `DataGrid` that binds to a collection of `InvoiceItem` and displays the required columns."_
