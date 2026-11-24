# Changelog

## 0.4.1 (2026-11-25)

- **The bar**, a third size of the float: one strip as tall as MapleStory's dashboard, the first time at the top of the
  game in its middle, just under the title bar. At its left your session's time with the map under it (a long name
  ends in "…"); then four tiles, EXP, mesos and kills an hour and the next level, with a thin blue line under the next
  level that shows how far your level is; then the round buttons (Pause; once paused, Continue and Reset) and at the
  right the size button and the cross. Large numbers make a tile wider, never cut off, and the bar keeps its width
  while the numbers change. While you are not in the game it says **Waiting** with what it waits for on two lines.
  Locked, it shows a small lock and "locked" where the buttons were. It does everything the float does: only above
  MapleStory, hidden with it and back with it, moving along with the game's window, held anywhere but on a button,
  light or dark as Windows is. It has a place of its own, and a drag sticks it to the game's middle as well as to its
  edges; when it gets wider or narrower it keeps the side it sticks to, or stays in the middle.
- **One button goes round the three sizes**: Small, Large, Bar, and Small again. It shows the three sizes as small
  shapes with the one you have filled in, and when you rest the pointer on it it says where a click takes you.
- Tempo's window shows its page also when the site sends it to the page in your language (gamemargins.com/nl/…), and
  the program never opens that page in your browser. On a Windows in another language the window could stay empty
  while the page opened in the browser.

## 0.4.0 (2026-11-24)

GameMargins Desktop is for two games now, and it has one look.

- **One look, light or dark as Windows is.** The window, the float and the menu by the clock are drawn by the program
  itself with the same parts: one font, one button, cards with a thin edge, switches. They follow Windows' app mode
  (Settings > Personalisation > Colours) at once, the Tempo window with them. Gold is only a page's main button and
  links; green means it works, orange watch out, red can't.
- **Appearance** on Settings: Follow Windows, Light or Dark. It holds for the window, the float, the menu by the
  clock and Tempo's window, at once.
- **The window is a list with pages**, as Tempo's: Overview, MapleStory Classic, WoW: Forever, and Settings at the
  foot with the version. Each page has one thing and at most one gold button. Every setting of the program is on
  Settings (Start with Windows, Keep the program up to date, Keep the addon up to date, Tempo may look at the
  MapleStory window, Appearance, Language, Add or remove a game, Unlink); the menu by the clock has none any more. A
  game you don't play has no page.
- **A first start in steps**, one a screen: your language (Windows' own is already set); your games; the link, with
  one gold button that opens gamemargins.com/link with the code filled in; and, with MapleStory Classic, the question
  **May Tempo look at your MapleStory window?** with **Yes, Tempo may look** and **No, not now**, in green what Tempo
  looks at and in red what it never does. With both games, WoW: Forever's link is a step of its own.
- **+ Add a game** in the list at the left while there is a game you have not turned on; turning one off is on
  Settings.
- **A short menu by the clock**: first how things are ("Tempo is reading · 26 min", "WoW: uploaded 3 min ago"), then
  what you can do: Open Tempo, Stop watching or Start watching, Upload now, Open GameMargins Desktop, Settings, Quit.
- **Notifications only when you have to do something**: an upload the site refused, the program unlinked on the
  site, an update that could not be installed and, with **Keep the program up to date** off, a version that waits.
  An upload that went well shows as the green check on the icon.
- **Updates install themselves quietly.** Afterwards Overview says "Updated to x" for a few days, with a link to what
  is new. The version before stays on your PC as the way back: when a new version does not stay up, the old one takes
  over again by itself and that version is not tried again. With **Keep the program up to date** off, a newer version
  waits on Settings with **Update now** and in the menu as **Update to x.y.z**.
- **What's new** beside that line opens gamemargins.com/whats-new at this version's entry.

- **Tempo for MapleStory Classic.** Tempo, gamemargins.com's tracker (EXP, mesos and kills an hour, the time to your
  next level, your monsters and drops), gets a window of its own in this program, titled "Tempo". The window shows the
  site's Tempo page; the program hands that page the picture of the MapleStory window, so no browser has to share a
  window and no "is sharing a window" bar shows.
- **What Tempo looks at, and what never.** Only the picture of the MapleStory window, as Windows composes it for your
  screen (Windows.Graphics.Capture, the way recording programs look at a window), without the mouse pointer (Windows
  10 before version 2004 can't leave the pointer out). The game's window is found by its title. Never the game's process, memory, files or network; never a message, a key or a click
  to the game; nothing drawn in or over the game. No picture leaves your PC: the page reads the numbers on your
  computer. The program shows this once and asks for your yes before Tempo first watches. `src/Capture.cs` holds the
  whole of it.
- **You always see when Tempo watches.** The tray icon carries a green dot, its tooltip says "Tempo is watching
  MapleStory", and its menu starts with "Tempo is reading" and has **Stop watching**. On Windows 11 nothing is drawn
  around the game; Windows 10 draws its own thin yellow border around a watched window, which a program can't remove.
- **Which Windows.** Tempo needs Windows 10 version 1903 or later, or Windows 11. On an older Windows 10 MapleStory
  Classic's page says "Tempo can't look at the game on this Windows", and WoW: Forever works as usual.
- **The float.** A small window over the game, in two sizes (the button next to the cross switches). Its head is
  your session's time as a stopwatch, with the map beside it. Large: the three numbers an hour, your level with its
  bar, your monsters, under them a row **Picked up here · not certain** that never scrolls away (what you picked up
  whose monster isn't certain: how many, and the five items with the most), and the buttons. Small: EXP, kills and
  mesos an hour and the next level, with a round button. The buttons work like a stopwatch: **Pause** while Tempo
  reads; once paused, **Continue** goes on with the same session and **Reset** keeps it and waits for **Start** to
  begin a new one. It keeps to the game as if it were part of it: on top of the game while the game is in front, and
  behind any other window you put over the game; it hides when you minimise the game and comes back once the game is
  there again; a drag sticks it to the game window's edges, inside and outside (also just under the title bar), and
  to the screen's; and when it is at the game it moves along when you move the game's window. Hold it anywhere but on
  a button to drag it. While you
  are not in the game it says **Waiting** and what it waits for. A click on the float never takes the keyboard away
  from the game, it is not in Alt-Tab, and it is never in the picture Tempo reads. **Lock the float (click through)**
  in Tempo's Settings lets clicks pass through it. Under every monster of the large size stand all its drops, a small
  picture and a count each; the arrow before a monster folds them away (until your next Reset). Rest the pointer on a
  picture and it says the item's name. Drag the large size by its bottom right corner to make it taller (more
  monsters) or wider: the width also makes everything in it larger, the pictures most of all (a monster's from 24 to
  42 pixels, an item's from 14 to 32), the letters a little; the size is remembered. A locked float shows a small lock and "locked" in its head and
  nothing that could be clicked: no buttons, no arrows, no Stop, no corner.
- **Which games do you play?** A first start asks once: WoW: Forever, MapleStory Classic or both. Who plays
  MapleStory alone is never asked for a WoW folder. A copy that was already in use is not asked: it stays as it is,
  for WoW, and **+ Add a game** in the list (or **Add or remove a game** on Settings) adds MapleStory.
- **Linking Tempo** works as for WoW: the program shows a code, you confirm it on gamemargins.com/link. Tempo has a
  token of its own (`gm_mt_…`), kept with Windows' protection like the WoW one; it never enters the page: the program
  adds it to the page's requests to the site's Tempo routes and to nothing else. Account › Security unlinks it.
- **Start Tempo with MapleStory**, a switch in Tempo's Settings that is off until you set it. Only while it is on (or the Tempo window is
  open) does the program look, every few seconds, whether a window titled "MapleStory" is open; when the game starts,
  Tempo opens minimised and starts watching.
- The Tempo window is Microsoft's WebView2 (Microsoft Edge's engine, part of Windows 11 and of an updated Windows 10).
  Three files of Microsoft's WebView2 SDK travel inside the exe (`lib/webview2`, under Microsoft's licence), so it is
  still one file with nothing to install, now about 1.4 MB and built for 64-bit Windows. The window's own data (cache)
  is in `%LOCALAPPDATA%\GameMargins\Uploader\webview2-data`. A Windows without WebView2 gets a line with
  Microsoft's download; the WoW side works without it.
- The program does not update itself while a Tempo session runs.
- WoW: Forever uploads as before; its accounts, folder, addon and last uploads are together on its page.

## 0.2.6 (2026-11-12)

- The notification after an upload names the auction house again. Windows' notification center shows a notification
  folded, with only the first line of its text, and cut "Uploaded: 244,569 auctions · EU · Normal · Horde" after
  "EU ·". Now "Uploaded: 244,569 auctions" is the notification's title and the auction house, "EU · Normal · Horde",
  has the line below it to itself. The program's name still stands above the notification.
- Every other notification follows the same rule: a title that says what happened and one short line that is seen
  whole, in all six languages. For example "Updated itself to 0.2.6" above "Turn this off in its menu.", "Upload
  failed" above "The file is over 12 MB." (the window still gives the full reason), "Version 0.2.7 is ready" above
  "Right-click the shield to update.".

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
