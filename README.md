# GameMargins Uploader

A small, free Windows program that uploads the GameMargins addon's auction house scans from World of Warcraft:
Forever to [gamemargins.com](https://gamemargins.com) by itself, the moment the game writes them, and keeps the addon
installed and up to date. The way the big addons' desktop apps do it, without a password ever entering the program.

Download it from [Releases](https://github.com/gamemargins/wow-uploader/releases) (each release lists the exe's
SHA-256) or from the [addon page](https://gamemargins.com/wow-forever/addon). It runs on Windows 10 and 11 with
nothing to install.

## What it does

- **Uploads by itself.** The addon saves its scans in `WTF\Account\<account>\SavedVariables\GameMargins.lua` when you
  log out, exit or `/reload`. The program notices the write, waits a few seconds for the game to finish, and sends the
  file to the site, for every WoW account in your install. **Upload now** in the window or the tray menu sends right
  away.
- **Keeps the addon up to date.** When the addon is missing, or the site has a newer version, the program installs it
  while WoW is closed (**Keep the addon up to date** in the tray menu, on by default; turn it off if CurseForge or Wago
  does this for you).
- **Lives by the clock.** Once linked it sits in the notification area with the GameMargins mark: a left click opens
  the window (per WoW account its status, the addon, the last uploads, **Upload now**), a right click the menu. For ten
  minutes after an upload that stored a scan the icon carries a small green check, and its tooltip says when the last
  upload was.
- **Notifications that stay.** A scan uploaded, an upload that failed, the addon installed or updated: Windows
  notifications, which stay in the notification center, also while Windows' Do not disturb is on during a game.
- **Tries again.** An upload that could not reach the site tries again by itself after 1, 2, 4 and 8 minutes, then
  every 15 minutes, until one goes through.
- **Speaks your language.** English, Dutch, German, French, Spanish and Brazilian Portuguese: your account's language
  once linked, the Windows language before that.

## What it reads and sends, and when

**Reads**, in your WoW: Forever folder (`C:\Program Files (x86)\World of Warcraft\_forever_` or `C:\Program
Files\World of Warcraft\_forever_`; if neither has accounts it asks once with a folder picker): the addon's
`GameMargins.lua` per account, and the version line of `Interface\AddOns\GameMargins\GameMargins.toc`. Nothing else in
the game folder is opened. It also looks at the names of the running programs, to leave the game folder alone while
WoW runs. It never touches the game itself, its memory or other addons.

**Sends**, only to the site it is linked to and only after you linked it:

- **The addon's file**, when the game wrote it and what the site keeps of it changed: the auction house scans, the
  trainers and vendors the addon noted, and per character the level, class, professions and recipes. The file as a
  whole is sent (as the site's own upload page does); the gold, bags, bank and "last seen" times the addon also writes
  are left out of the check, and the site never keeps them. **Upload now** sends regardless; the site recognises a
  scan it already has and doesn't store it twice.
- **A status check** at start and right after linking: whether the token is still good, your account's language, the
  newest program version (the window says when a newer one is out; it never updates itself) and the addon zip the site
  serves, with its version, size and SHA-256.
- **The addon zip download**, from the linked site only: refused unless its size and SHA-256 match what the status
  said, and unless it holds only the `GameMargins` folder of that version.

Every request carries `GameMarginsUploader/<version> (Windows)` as its user agent; the status check and the upload
also carry the token. Your account folders are Battle.net account names: they never leave your computer, are never
written to a file, and the window calls them "WoW account 1, 2, …". Like any connection to a website, the address you connect from is visible to the site.

Details: the [privacy policy](https://gamemargins.com/privacy).

## Linking

The program shows a short code, with a button that opens `gamemargins.com/link` and one that copies the link. Log in
there, enter or check the code, and press **Link**: within a few seconds the program receives a token of its own and
starts uploading. No password or mail code is typed into the program, and your authenticator keeps working as before.

The token (`gm_wu_…`) can only upload scans: it can't read your trades, your account or anything else. It is kept in
`%LOCALAPPDATA%\GameMargins\Uploader\settings.json`, protected with Windows' own DPAPI for your Windows user (another
user or another computer can't read it), never in plain text. Unlink it with one click on your account's Security page
(**Account › Security** on the site): at its next start or upload the program says so and shows a new code.

## What it changes on your PC

It runs as a normal user program and never asks for administrator rights.

- `%LOCALAPPDATA%\GameMargins\Uploader\`: `settings.json` (the site, your WoW folder, your account's language, the
  two menu ticks, the protected token, fingerprints of the files already uploaded and the last eight uploads for the
  window; no log file) and `logo.png`, the picture the notifications show.
- **Start with Windows**: the value `GameMarginsUploader` under
  `HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Run`, only while **Start with Windows** in the tray menu
  is ticked. It is ticked at the first link; unticking it removes the value. While it is ticked, a start of the
  program points the value at the exe that runs, so a newer download takes over autostart.
- **A Start-menu shortcut**, `%APPDATA%\Microsoft\Windows\Start Menu\Programs\GameMargins Uploader.lnk`, pointing at
  the exe and carrying the program's AppUserModelID (`GameMargins.Uploader`). Windows needs it to show the program's
  notifications with its name and icon; it is rewritten when the exe moved, and it makes the program findable in the
  Start menu.
- **The addon**, only while **Keep the addon up to date** is ticked and WoW is closed: `Interface\AddOns\GameMargins`
  in your WoW: Forever folder. A new version is unpacked in `Interface\GameMargins-staging` first (removed afterwards);
  the old folder moves to `Interface\GameMargins-backup\GameMargins-<old version>` (the last three are kept). Nothing
  else under `Interface\AddOns` is read or written.

To remove it: untick **Start with Windows**, choose **Quit**, unlink it under Account › Security, and delete the exe,
the Start-menu shortcut and `%LOCALAPPDATA%\GameMargins\Uploader`.

## Building

On Windows 10 or 11, with nothing to install (the C# compiler of the .NET Framework that Windows ships):

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File build.ps1
```

Output: `dist\GameMarginsUploader.exe`, and the same file as `dist\GameMarginsUploader-<version>.exe`, the name a
release publishes; the build prints its SHA-256. The version is `Program.Version` in `src\Program.cs`, the one place
it is written: `src\AssemblyInfo.cs` stamps it into the exe's file data (Properties › Details), and the build checks
that. The sources keep to C# 5, which that compiler speaks.

The release builds are not code-signed, so Windows SmartScreen warns on the first start ("Windows protected your PC"
→ More info → Run anyway). Compare the file's SHA-256 with the one on the release page if in doubt.

Command-line options, for testing: `--site http://localhost:3000` points one start at another copy of the site (never
remembered; a token from another site is dropped, and autostart is left alone), `--wow C:\path\to\_forever_` at
another WoW folder (remembered as the program's folder), and `--test-toast` shows one test notification and writes the result to
`%TEMP%\GameMargins-toast-test.txt`.

## Reporting a problem

Open an [issue](https://github.com/gamemargins/wow-uploader/issues) with the program's version (in the exe's
Properties › Details, or the release you downloaded), your Windows version, and what the window or the notification
said. Never post your `settings.json` or a token. For anything about your account, or a security problem you would
rather not report in public, use the [contact page](https://gamemargins.com/contact).

## License

BSD 2-Clause, see `LICENSE`.

The license covers the code. The GameMargins name and logo (including `GameMargins.ico`) are not covered by it: please
give a modified build its own name and icon.
