# Changelog

## 0.2.5 (2026-11-11)

- **GameMargins Desktop** is the program's new name (until now GameMargins Uploader): in the window, the notifications,
  the tray, the exe's file data, the download (`GameMarginsDesktop-0.2.5.exe`), the Start-menu shortcut
  (`GameMargins Desktop.lnk`; the old `GameMargins Uploader.lnk` is removed when it points at a GameMargins exe) and the
  user agent (`GameMarginsDesktop/0.2.5 (Windows)`). This repository is now github.com/gamemargins/desktop. The
  settings, the link to your account and autostart stay as they were.
- Keeps itself up to date: **Keep the program up to date** in the tray menu, on by default. At start and once a day
  the program reads `https://gamemargins.com/wow-forever/uploader.json` (the newest version, its exe on this
  repository's releases and the exe's SHA-256). A newer version is downloaded from
  `https://github.com/gamemargins/desktop/releases/download/` only, kept only when its SHA-256 matches and its
  file data names that version, and saved as `%LOCALAPPDATA%\GameMargins\Uploader\GameMarginsDesktop-<version>.exe`.
  Autostart (while ticked) and the Start-menu shortcut move to it, it starts, the old copy quits, and a notification
  says "Updated to x.y.z". An upload under way finishes first. A download that doesn't match keeps nothing and says
  so, with the site to download it from instead (an antivirus web filter can replace or cut a download).
- With the tick off, a newer version shows as **Update to x.y.z** in bold at the top of the menu, with one
  notification; one click does the same update.
- With the tick on, an older copy started from elsewhere (an old download on the desktop) hands over to the newer
  one installed in that folder.
- The tray icon's tooltip names the version.

## 0.2.4 (2026-11-10)

The first release on GitHub.

- The download carries its version in its file name (`GameMarginsUploader-0.2.4.exe`), so two downloads side by side
  can be told apart.
- The exe's file data (Properties › Details) names the program, GameMargins as its maker, the copyright and the
  version (0.2.4.0), where earlier builds said "File version 0.0.0.0".
- Autostart follows the exe: with **Start with Windows** ticked, a start writes the Run value again when it points
  at another file, so a newer download takes over autostart. With it unticked nothing is written.

## 0.2.3 (2026-11-10)

- Notifications are Windows toasts that stay in the notification center, also under Do not disturb (balloon tips
  were dropped without a trace while a game ran). The program makes a Start-menu shortcut for them.
- For ten minutes after an upload that stored a scan, the tray icon carries a green check, and its tooltip says when
  the last upload was.
- An upload that could not reach the site tries again by itself after 1, 2, 4 and 8 minutes, then every 15 minutes.

## 0.2.2 (2026-11-10)

- A level-up, a new profession skill or new recipes upload too: the check for "changed" now keeps what the site keeps
  of a character, and still leaves out the gold, bags, bank and times the addon writes at every logout.

## 0.2.1 (2026-11-10)

- **Copy link** beside **Open** on the link card, for a browser the program does not open by itself.

## 0.2.0 (2026-11-09)

- Lives in the notification area with the GameMargins mark; the window opens with a left click.
- Installs the addon when it is missing and updates it when the site has a newer version, only while WoW is closed,
  checked against the size and SHA-256 the site names, with a backup of the old version. The zip comes from the
  linked site only, and an installed addon is never replaced by an older version.

## 0.1.0 (2026-11-09)

- Watches the addon's file in every WoW account and uploads it the moment the game writes it, linked to the account
  with a code on gamemargins.com/link.
