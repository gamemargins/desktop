# Changelog

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
