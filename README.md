# Papra Companion

A self-hosted Blazor Server companion application for [Papra](https://github.com/papra-hq/papra), the open-source document management system. Papra Companion listens for document upload webhooks and automatically enriches documents with AI-generated titles and tags. It also includes an optional IMAP-based email attachment downloader that feeds files directly into Papra.

---

## Features

- **Webhook-driven AI pipeline** — receives a webhook from Papra on every document upload and automatically:
  1. Downloads the document file and metadata via the Papra API
  2. Performs OCR / text extraction (via Mistral OCR or OpenAI vision)
  3. Generates a descriptive document title using OpenAI
  4. Selects matching tags from your Papra tag library using OpenAI
  5. Writes the title and tags back to Papra
- **Email attachment downloader** — polls an IMAP mailbox on a configurable schedule, downloads matching attachments, and saves them to Papra's watched folder
  - Subject-line regex filtering (with optional case-insensitive / match-anywhere modes)
  - Custom filename templates
  - Optional delete-after-download (with copy-to-folder support)
- **Pipeline dashboard** — real-time job status, success/failure badges, extracted titles, and per-job timings

---

## Prerequisites

| Requirement | Notes |
|---|---|
| [Papra](https://github.com/papra-hq/papra) instance | Must be reachable from the Companion container |
| OpenAI API key | Required for title extraction and tag suggestion |
| Mistral API key | Optional — enables Mistral OCR (falls back to OpenAI vision if absent) |
| Docker + Docker Compose | Recommended deployment method |

---

## Quick Start (Docker Compose)

1. **Create a `docker-compose.yml`** with the following content:

   ```yaml
   services:
     papra-companion:
       image: ghcr.io/remotesojourner/papra-companion:latest
       ports:
         - "1003:1003"
       volumes:
         - ./data:/app/data
         - /path/to/papra/ingestion:/app/attachments
   ```

   **Volume notes:**
   - `./data` — stores the SQLite database and Data Protection keys on the host next to your compose file. Created automatically on first run.
   - `/path/to/papra/ingestion` — replace this with the path to the folder that Papra watches for new documents. Please not that it needs to have the organisation id at the end. e.g. /papra/consume/org_bwnnm9xppyw81ru5r3wvvr5d. Any attachment downloaded from email will be dropped here and picked up by Papra automatically.

2. **Start the stack**

   ```bash
   docker compose up -d
   ```

   The app will be available at **http://localhost:1003**.

3. **Configure via the Settings page**

   Open the browser, navigate to **Settings**, and complete the four configuration tabs (see [Configuration](#configuration) below).

4. **Register the webhook in Papra**

   In Papra, go to **Settings → Webhooks** and add the webhook URL shown on the Papra settings tab:

   ```
   http://<companion-host>:1003/webhook/document
   ```

   From this point on, every document uploaded to Papra will automatically be processed by the pipeline.

---

## Configuration

All configuration is stored in the SQLite database and managed entirely through the browser-based Settings UI. No environment variables or config files need to be edited manually.

### Tab: Papra

| Field | Description |
|---|---|
| **Base URL** | Root URL of your Papra instance, e.g. `https://documents.example.com` (no trailing slash) |
| **API Token** | Bearer token from Papra's user settings page |
| **Webhook URL** | Read-only display — copy this into Papra's webhook configuration. Make sure the event being sent is "Document created" |

Use the **Test Connection** button to verify that the Companion can reach your Papra instance and authenticate successfully before saving.

### Tab: AI Services

| Field | Description |
|---|---|
| **OpenAI API Key** | Required. Used for title generation, tag suggestion, and OCR fallback |
| **OpenAI Model** | Model name, e.g. `gpt-4o-mini` (default) or select another from the dropdown |
| **Mistral API Key** | Optional. When present, Mistral OCR is used for text extraction instead of OpenAI vision |


### Tab: AI Prompts

Fully customizable system prompts sent to OpenAI. Three prompts are configurable:

| Prompt | Purpose | Available placeholders |
|---|---|---|
| **Title prompt** | Instructs the model to produce a document title | `{{original_title}}`, `{{content}}` |
| **Tag prompt** | Instructs the model to select tags from the available list | `{{available_tags}}`, `{{original_title}}`, `{{content}}` |
| **OCR prompt** | Instructs the model to extract plain text from the document | *(none)* |

Default prompts are provided and can be restored at any time.

### Tab: Email

Configures the IMAP attachment downloader background service.

| Field | Description |
|---|---|
| **Enabled** | Toggles the background polling service on/off |
| **Host** | IMAP server hostname |
| **Port** | Default: `993` |
| **Username / Password** | IMAP credentials |
| **Use SSL** | Enable implicit SSL/TLS (recommended) |
| **Use STARTTLS** | Enable STARTTLS (for servers that don't use implicit SSL) |
| **IMAP Folder** | Mailbox folder to poll, e.g. `INBOX` |
| **Subject Regex** | Regular expression to filter emails by subject line |
| **Case-insensitive** | Apply the regex case-insensitively |
| **Match anywhere** | Match the regex anywhere in the subject (not just from the start) |
| **Filename Template** | Template for naming saved attachments |
| **Delete after download** | Remove the email from the server after downloading its attachments |
| **Copy-to folder** | If deleting, first copy the message to this IMAP folder |
| **Poll interval** | How often to check for new mail, in seconds (default: `300`) |

---

## Development Setup

### Requirements

- .NET 10 SDK
- No Node.js needed — Tailwind CSS is downloaded automatically

### Run locally

```bash
cd Papra.Companion
dotnet run
```

The first build will download the Tailwind CSS standalone CLI (`tailwindcss.exe` / `tailwindcss` on Linux/macOS) to the project directory and generate `wwwroot/css/app.min.css`. The binary is git-ignored.

### Tailwind CSS

Tailwind v4 is configured as a standalone CLI pipeline:

- The CLI binary is downloaded by an MSBuild target (`DownloadTailwind`) on first build
- The `Tailwind` MSBuild target runs the CLI to compile `wwwroot/css/app.css` → `wwwroot/css/app.min.css`
- The source `app.css` is excluded from the published output (only the compiled `app.min.css` is deployed)
- The pinned version is set via `<TailwindVersion>` in the `.csproj`

### Database migrations

EF Core migrations are applied automatically on startup via `MigrateAsync()`. To add a new migration during development:

```bash
dotnet ef migrations add <MigrationName> --project Papra.Companion.Data --startup-project Papra.Companion
```

---

## Technology Stack

| Component | Technology |
|---|---|
| Framework | ASP.NET Core 10, Blazor Server (Interactive Server render mode) |
| UI components | [Flowbite Blazor](https://flowbite-blazor.com/) |
| CSS | Tailwind CSS v4 (standalone CLI) |
| Database | SQLite via Entity Framework Core |
| HTTP clients | `System.Net.Http.Json` with typed DTOs |
| IMAP | [MailKit](https://github.com/jstedfast/MailKit) |
| Containers | Docker + Docker Compose |

---

## License

This project is provided as-is. See [LICENSE](LICENSE) for details.