# 🔥 Ognia

<p align="center">
  <img src="Frontend/Assets/Brand/Icon/Ognia.png" alt="Ognia logo" width="200"/>
</p>

A Windows music player that turns YouTube links into a local, offline-capable library. Personal learning project — ASP.NET Core backend for accounts and metadata, WPF client that downloads and plays audio.

## Screenshots

| Login | Library |
|:---:|:---:|
| <img src="https://github.com/user-attachments/assets/86eeb897-f972-4196-b861-edccf7a2a38b" width="400" /> | <img src="https://github.com/user-attachments/assets/3165d5d5-70d2-4b0a-b6d2-30e3b888feab" width="400"/> |

| Playlist | Settings |
|:---:|:---:|
| <img src="https://github.com/user-attachments/assets/d16d8f42-f1f8-4e40-8271-2846e2eb0544" width="400"/> | <img width="400" src="https://github.com/user-attachments/assets/f596fd7b-c906-4f42-91bc-65e44477b395" /> |

| Mini Player (Active) | Mini Player (Inactive) |
|:---:|:---:|
| <img width="400" alt="image" src="https://github.com/user-attachments/assets/59c66794-54da-4a83-97c7-122b9f219ace" /> | <img width="400" src="https://github.com/user-attachments/assets/f50fdd56-57e3-4069-ad65-88450d97ecf1" /> |



## ✨ Features

- Email/password accounts with JWT + refresh tokens
- Forgot password / reset password / change password
- Guest mode: use the app with no backend, stored locally
- Guest-to-account upgrade (uploads the local library on first login)
- Export / import the guest library as JSON
- Playlists: create, rename, delete, reorder
- Tracks: add by URL, rename, remove
- Download audio to a local folder, play offline
- Play/pause, stop, next/previous, seek, volume, mute
- Repeat (off/all/one) and shuffle
- Download all tracks in a playlist, with status bar progress
- Auto-generated 2×2 playlist covers from track thumbnails
- Mini player: compact, always-on-top window with hover-to-reveal fade
- Themes: Default, Dark, Light (JSON-based, user-editable)

## 🏗️ Architecture

```
+----------------+   HTTPS/JSON   +------------------+
|  WPF (client)  | <------------> |  ASP.NET Core    |
|                |  JWT bearer    |  (backend)       |
+----------------+                +------------------+
       |                                    |
       v                                    v
  Local audio +                       SQLite database
  settings + library                  (users, playlists,
  + themes + cache                    tracks, tokens)
```

The backend owns users, playlists, and track metadata. The client owns local audio files, settings, themes, and (in guest mode) a local library. The backend never handles audio bytes — the client downloads directly from YouTube via `YoutubeExplode`. Metadata lookups (title, duration, thumbnail) also go through `YoutubeExplode`: the backend does them on behalf of logged-in users, and the client does them locally in guest mode.

## 🧰 Tech stack

- **Backend:** .NET 10, ASP.NET Core Web API, EF Core + SQLite, ASP.NET Core Identity, JWT bearer, Swashbuckle
- **Frontend:** .NET 10, WPF, plain MVVM (`INotifyPropertyChanged`, `ICommand`), NAudio, `YoutubeExplode`

## 🚀 Getting started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- Windows (WPF client is Windows-only)
- Optional: `dotnet tool install --global dotnet-ef`

### Backend

```bash
cd Backend
dotnet user-secrets set "Jwt:Key" "<64-char-random-hex>"
dotnet ef database update
dotnet run
```

Generate a key with:

```powershell
[Convert]::ToHexString((1..32 | ForEach-Object { Get-Random -Max 256 })).ToLower()
```

Backend runs at `https://localhost:7235`. Swagger at `/swagger`.

### Frontend

```bash
dotnet dev-certs https --trust     # one-time
cd Frontend
dotnet run
```

The client connects to `https://localhost:7235` by default. To change it, set `api.baseUrl` in `%LOCALAPPDATA%\Ognia\settings.json` after first launch (or edit `ApiSettings.BaseUrl` in `Frontend/Helpers/AppSettings.cs` before building).

### 🎧 First use

1. **Continue as Guest** to try without a backend account, or register.
2. Create a playlist.
3. Paste a YouTube URL.
4. Click **Download** to fetch audio locally.
5. Click **Play**.

## 📁 Where files live

**Audio** — `%USERPROFILE%\Music\Ognia\<playlist>\<track>.m4a` (per-playlist folder).

**App data** — `%LOCALAPPDATA%\Ognia\`:

| File | Purpose |
|---|---|
| `settings.json` | App mode, active theme, music root; playback state (volume, repeat, shuffle, last track + position); window geometry; last opened playlist |
| `token.json` | JWT + refresh token + expiry |
| `library.json` | Guest-mode playlists and tracks |
| `Themes/` | Theme JSON files (seeded from embedded defaults) |
| `playlist-covers/` | Generated 2×2 cover collages |

**Database** — `Backend/database.db` (SQLite, created by `dotnet ef database update`).

## 🎵 Track sources

Each track has a `(SourceName, SourceId)` pair — e.g. `("youtube", "dQw4w9WgXcQ")`. All of these URLs normalize to the same track:

- `https://www.youtube.com/watch?v=dQw4w9WgXcQ`
- `https://youtu.be/dQw4w9WgXcQ`
- `https://music.youtube.com/watch?v=dQw4w9WgXcQ&t=42s`

### ➕ Adding a new source

Implement `ITrackSourceService` in the backend (see `Backend/Services/TrackSourceService/`), add a `TrackSource` enum member, add its wire name to `TrackSourceExtensions`, and register it in `Program.cs`.

Note: the backend currently registers a single `ITrackSourceService` (`YouTubeTrackSourceService`) and the add-track endpoint uses that one instance directly. To support multiple sources, change the registration to a collection (`IEnumerable<ITrackSourceService>`) and update the endpoint to dispatch by `CanHandle(url)` — the interface is already designed for this.

## 🤝 Contributing

Contributions are welcome — this started as a personal project, and it's better with more eyes on it.

### 🐛 Found a bug or want a feature?

Open an [issue](https://github.com/XxDaShTixX/Ognia/issues). Both are equally welcome.

- **Bugs** — describe what you expected, what happened, and how to reproduce it. Screenshots or stack traces help.
- **Features** — describe the use case, not just the solution. "I want to X so that Y" is more useful than "add a button called Z."

### 🛠️ Submitting code

1. **Fork** the repo (or, if you have access, branch directly).
2. **Create a branch named after yourself and the change:**
   ```bash
   git checkout -b YourName-ShortDescription
   # e.g. Dash-FixPlaylistRename, Dash-AddKeyboardShortcuts
   ```
3. **Make your change.** Keep commits focused; a single feature or fix per branch is ideal. The smaller the change, the better.
4. **Test it.** `dotnet build` must pass. If you touched the backend, run it and click through the flow. If you touched the client, run it with both guest mode and an account.
5. **Open a pull request** against `main`. Describe what changed and why. Reference the issue number if there is one (`Closes #12`).

### 🤖 AI-assisted contributions

Using AI tools (Copilot, Claude, ChatGPT, Cursor, etc.) is fine. You don't need to disclose it. What matters is that you understand what you're submitting — you should be able to explain every line of your diff, debug it if it breaks, and stand behind it in review. "The model wrote it" is not a defense when something goes wrong.

### 🔍 What I'll look at in a PR

- Does it work? Build clean, no errors or warning, no obvious regressions.
- Does it fit the existing structure? MVVM in the client, DTOs in the backend, `ITrackSourceService` for new sources.
- Does it work in guest mode too? Anything user-facing should either function with `LocalMusicRepo` or be explicitly hidden when no backend is configured.
- Is the diff small? Large sweeping refactors are harder to review than focused changes.

### 🚫 What I won't merge

- Secrets in commits (JWT keys, API tokens, anything in `appsettings.json` that looks like a real credential).
- Bypassing guest mode when it's trivial to support.
- New source integrations that violate a platform's terms of service.

### 🧭 Not sure where to start?

[Open issues](https://github.com/XxDaShTixX/Ognia/issues) tagged `General` are a good entry point. Or open an issue describing what you'd like to build and we'll figure out the approach together before you write code.

### 🌱 No contribution is too small

Typo fixes, README improvements, clarifying a comment — all welcome. You don't need to rewrite a subsystem to help.

## ⚖️ Legal

This is an educational project. It does **not** host, distribute, or own any copyrighted audio. Audio is downloaded by the client from YouTube, subject to YouTube's terms of service and applicable copyright law. `YoutubeExplode` is used for interoperability.

If you distribute a build:

- YouTube's terms prohibit downloading content for most uses.
- App stores (Apple, Google) generally reject apps with built-in YouTube-downloading.
- Distributing on GitHub sidesteps store review but does not change the underlying legal risk.

Use it for personal learning and experimentation. Don't redistribute content you don't have rights to.

## 📜 License

MIT — see [LICENSE](LICENSE).
