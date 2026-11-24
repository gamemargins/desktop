# GameMargins Desktop

A small, free Windows program for [gamemargins.com](https://gamemargins.com), for two games:

- **MapleStory Classic.** Tempo, our tracker (EXP, mesos and kills an hour, the time to your next level, your monsters
  and drops), runs in a window of its own with a small float over the game. It reads the picture of the game's window
  on your PC and nothing else of the game.
- **World of Warcraft: Forever.** It uploads the GameMargins addon's auction house scans by itself, the moment the
  game writes them, and keeps the addon up to date.

At its first start it asks which of the two you play; add or remove one later under Settings. It keeps itself up to
date, and no password ever enters the program: you link it with a code on gamemargins.com.

Download it from [Releases](https://github.com/gamemargins/desktop/releases) (each release lists the exe's SHA-256)
or from [gamemargins.com](https://gamemargins.com/desktop). It runs on Windows 10 and 11 with nothing to install
(which versions of Windows 10: see "Windows 10 and 11"). Until 0.2.5 it was called GameMargins Uploader, and this
repository gamemargins/wow-uploader (its addresses still lead here); updating keeps your settings, your link and
autostart.

## What it does

- **Uploads by itself.** The addon saves its scans in `WTF\Account\<account>\SavedVariables\GameMargins.lua` when you
  log out, exit or `/reload`. The program notices the write, waits a few seconds for the game to finish, and sends the
  file to the site, for every WoW account in your install. **Upload now** on WoW: Forever's page or in the menu by the
  clock sends right away.
- **Keeps the addon up to date.** When the addon is missing, or the site has a newer version, the program installs it
  while WoW is closed (**Keep the addon up to date** on Settings, on by default; turn it off if CurseForge or Wago
  does this for you).
- **Keeps itself up to date, quietly.** At start and once a day it asks gamemargins.com which version is newest; a
  newer one is downloaded from this repository's releases, checked, and takes over while the window is closed
  (**Keep the program up to date** on Settings, on by default). No notification: Overview says "Updated to x" for a
  few days, with **What's new**, a link to that version's entry on gamemargins.com/whats-new. An upload, an addon
  install or a Tempo session under way finishes first. Turn the switch off and nothing is downloaded by itself: a
  newer version waits as a row on Settings, "Version x.y.z is ready" with **Update now**, and as **Update to x.y.z**,
  the first thing to do in the menu; one notification says so.
- **A way back.** The version an update replaced stays on your PC. When a new version does not stay up (it ends with
  an error within 30 seconds of taking over), the one before starts again by itself, says so in a notification
  ("Could not update to x.y.z"), and that version is not tried again.
- **Tempo for MapleStory Classic.** A window titled "Tempo" shows gamemargins.com's Tempo
  page; the program hands that page the picture of the MapleStory window, so no browser has to share a window. A small
  float stays on top of the game with your session's time, your numbers an hour and your monsters with their drops,
  in three sizes: Small, Large, and a bar as tall as the game's dashboard, with one button that goes round them; a
  click on it never takes the keyboard away from the game. **Start Tempo with MapleStory**, **Show
  the float** and **Lock the float (click through)** are in the Tempo window's own Settings.
- **One window, a page per thing.** A list at the left: Overview, a page per game you use, and Settings at the foot
  with the version. Each page has one main button. Overview has a card per game with one line that says how it is
  doing. WoW: Forever's page has **Upload now**, your WoW accounts, the folder, the addon and the last uploads.
  MapleStory Classic's page has **Open Tempo**, how Tempo is doing, whether the game's window was found, whether
  Tempo may look, and the float. **+ Add a game** stands in the list while there is a game you have not turned on.
- **Every setting on Settings**: Start with Windows, Keep the program up to date, Keep the addon up to date, Tempo
  may look at the MapleStory window, Appearance (Follow Windows, Light or Dark), Language, Games (Add or remove a
  game) and Account (Unlink). A setting of a game you don't use is not shown.
- **A short menu by the clock.** The program sits in the notification area with the GameMargins mark: a left click
  opens the window, a right click the menu. The menu first says how things are ("Tempo is reading · 26 min", "WoW:
  uploaded 3 min ago"), then what you can do: Open Tempo, Stop watching or Start watching, Upload now, Open
  GameMargins Desktop, Settings, Quit. It holds no settings.
- **A green check, not a notification.** For ten minutes after an upload that stored a scan the icon carries a small
  green check, and its tooltip says when the last upload was. While Tempo watches the game the icon carries a green
  dot and the tooltip says "Tempo is watching MapleStory".
- **Notifications only when you have to do something**: an upload the site refused, the program unlinked on the
  site, an update that failed its check or could not be installed, and, with **Keep the program up to date** off, a
  version that waits. They are Windows notifications, which stay in the notification center, also while Windows' Do
  not disturb is on during a game.
- **A first start in steps**, one thing a screen: your language (the one Windows uses is set already); **Which games
  do you play?** (WoW: Forever, MapleStory Classic or both); the link, with one button that opens
  `gamemargins.com/link` with the code filled in, a step of its own per game you ticked; and, with MapleStory
  Classic, the question **May Tempo look at your MapleStory window?** as the last step. A copy that was already in
  use for WoW: Forever is not asked: **+ Add a game** adds MapleStory Classic.
- **Tries again.** An upload that could not reach the site tries again by itself after 1, 2, 4 and 8 minutes, then
  every 15 minutes, until one goes through.
- **Speaks your language.** English, Dutch, German, French, Spanish and Brazilian Portuguese: the Windows language at
  first, your account's language once linked, or the one you set on Settings.
- **Light or dark as Windows is**, or the one you set under Appearance. The window, the float, the menu and the Tempo
  window follow together.

One known limit: the program draws its own pages, so a screen reader hears only the window's title.

## What it reads and sends, and when

### World of Warcraft: Forever

**Reads**, in your WoW: Forever folder (`C:\Program Files (x86)\World of Warcraft\_forever_` or `C:\Program
Files\World of Warcraft\_forever_`; if neither has accounts, WoW: Forever's page says so and offers **Choose
folder…**): the addon's `GameMargins.lua` per account, and the version line of
`Interface\AddOns\GameMargins\GameMargins.toc`. Nothing else in the game folder is opened. It also looks at the names
of the running programs, to leave the game folder alone while WoW runs. It never touches the game itself, its memory
or other addons.

**Sends**, only to the site it is linked to and only after you linked it:

- **The addon's file**, when the game wrote it and what the site keeps of it changed: the auction house scans, the
  trainers and vendors the addon noted, and per character the level, class, professions and recipes. The file as a
  whole is sent (as the site's own upload page does); the gold, bags, bank and "last seen" times the addon also writes
  are left out of the check, and the site never keeps them. **Upload now** sends regardless; the site recognises a
  scan it already has and doesn't store it twice.
- **A status check** at start and right after linking: whether the token is still good, your account's language, the
  newest program version, and the addon zip the site serves, with its version, size and SHA-256.
- **The addon zip download**, from the linked site only: refused unless its size and SHA-256 match what the status
  said, and unless it holds only the `GameMargins` folder of that version.

Your account folders are Battle.net account names: they never leave your computer, are never written to a file, and
the window calls them "WoW account 1, 2, …".

### Tempo, for MapleStory Classic

The whole of what the program looks at is in [`src/Capture.cs`](src/Capture.cs); the Tempo window is
[`src/TempoWindow.cs`](src/TempoWindow.cs).

**Looks at**: the picture of the MapleStory window, as Windows composes it for your screen, through
Windows.Graphics.Capture (the way recording and streaming programs look at a window). The mouse pointer is left out
of the picture on Windows 11 and on Windows 10 from version 2004; an older Windows 10 can't leave it out, so there
the pointer is in the picture while it is over the game. Only after you said yes to **May Tempo look at your
MapleStory window?** in the program; **Tempo may look at the MapleStory window** on Settings takes that yes back.

**Finds the game's window** by asking Windows for the open windows' titles, classes, sizes and whether they are shown
or minimised; the game's is the one titled exactly "MapleStory". The program looks for it only after that yes, and
only while **Start Tempo with MapleStory** is on or the Tempo window is open, every few seconds. None of this asks
anything of the game.

**Never**: the game's process, memory, modules, files or network; never a message, a key or a click to the game;
no hooks; nothing drawn in or over the game.

**No picture leaves your PC.** The program never looks inside a picture itself. The frames go, in memory on your
computer, one frame at a time, to the Tempo page in the program's own window, and the page reads the numbers there.
Nothing of a frame is written to disk or sent anywhere.

**You always see when it watches**: a green dot on the icon by the clock, "Tempo is watching MapleStory" as its
tooltip, and **Stop watching** in its menu.

**The Tempo window** is Microsoft's WebView2 (Microsoft Edge's engine). It loads the Tempo page from gamemargins.com,
as a browser would, and only that page: any other link opens in your own browser. The page can't share a screen or a
window by itself, nor use the camera, the microphone or your location.

**Sends**, to gamemargins.com only, and only after you linked the program for Tempo:

- **A start check**: whether Tempo's token is still good and whether the account may use Tempo; a yes comes with the
  address of the Tempo page. It is asked right after Tempo's link, at every start of the program, once a day, each
  time the Tempo window opens, and when you press **Check again**.
- **What the Tempo page sends**, the same as Tempo in a browser: when you stop a session, its summary (times, the
  map, EXP, mesos, kills and drops per monster, quests turned in, what you crafted, and the kills and drops counted
  for the site's Drop database); the shop prices it read while a player shop was open in the game; and, only after
  you click **Put my shop online**, what your own shop offers. Numbers and the game's ids: never a picture, never the
  chat, never a name, except your character's name beside your shop when you turn that on. It also asks the site for
  prices and for your kept sessions.

The [privacy policy](https://gamemargins.com/privacy) says what the site keeps of these and for how long, under
"Tempo in GameMargins Desktop" and the items around it.

### Both

**Fetches**, linked or not:

- **The version file** `https://gamemargins.com/wow-forever/uploader.json`, at start and once a day: the newest
  version, the address of its exe and the exe's SHA-256. That address only; the request carries no token and nothing
  about you or the game. With **Keep the program up to date** off a newer version only shows as **Update now**, and
  nothing is downloaded before that click.
- **A newer version's exe**, only from `https://github.com/gamemargins/desktop/releases/download/` over HTTPS
  (GitHub sends the download on to its own host, `*.githubusercontent.com`; nothing else is followed), at most 10 MB.
  It is kept only when its SHA-256 is the one the version file names and its file data (Properties › Details) names
  that version; otherwise nothing is kept and a notification says so and names gamemargins.com to download it from (an
  antivirus web filter can replace or cut a download of this unsigned exe). Never an older version, and nothing when
  the version can't be read. Like any download, GitHub sees the address you connect from.

Every request of the program carries `GameMarginsDesktop/<version> (Windows)` as its user agent; the Tempo window
adds `GameMarginsDesktop/<version>` to WebView2's own. The status check and the upload carry the WoW token; Tempo's
start check and the Tempo page's requests to the site's Tempo routes carry Tempo's token. Like any connection to a
website, the address you connect from is visible to the site.

Details: the [privacy policy](https://gamemargins.com/privacy).

## Linking

The program shows a short code, with a button that opens `gamemargins.com/link` with the code filled in and one that
copies the link. Log in there, check the code, and press **Link**: within a few seconds the program receives a token
of its own. No password or mail code is typed into the program, and your authenticator keeps working as before. A
code works for ten minutes.

There are two links, one per game, each with its own code and its own token:

- **WoW: Forever**: the token (`gm_wu_…`) can only upload scans and ask for the status above. It can't read your
  trades, your account or anything of Tempo.
- **Tempo**: the token (`gm_mt_…`) opens the site's Tempo routes (`/api/maplestory/tempo-…`) and nothing else,
  nothing of WoW either. It never enters the Tempo page: the program adds it to the page's requests to those routes
  and to no other request.

Both tokens are kept in `%LOCALAPPDATA%\GameMargins\Uploader\settings.json`, protected with Windows' own DPAPI for
your Windows user (another user or another computer can't read them), never in plain text. Unlink either with one
click on your account's Security page (**Account › Security** on the site): at its next start, upload or Tempo
request the program says so and shows a new code. **Unlink** on the program's Settings makes this PC forget both
tokens; revoking them is done on the site.

## What it changes on your PC

It runs as a normal user program and never asks for administrator rights.

- `%LOCALAPPDATA%\GameMargins\Uploader\`:
  - `settings.json`: the site, your WoW folder, the language, the switches of Settings, which games you use, both
    protected tokens, whether you said yes to Tempo looking at the game's window, Tempo's own switches, where the
    Tempo window, the float and the bar stood and the float's size, the version the program last updated itself to and when,
    a version that did not stay up, the newest version a "Version x.y.z is ready" notification announced,
    fingerprints of the files already uploaded and the last eight uploads for the window.
  - `GameMarginsDesktop-<version>.exe`: the versions the program installed itself. After an update the running one
    and the one it replaced are kept (the way back); older ones are removed from there. A copy elsewhere, such as
    your download, is left alone.
  - `webview2-data\`: the Tempo window's own data, as a browser keeps it (WebView2's cache and the Tempo page's
    storage); it can grow to some tens of MB.
  - `webview2\<SDK version>\WebView2Loader.dll`: Microsoft's loader for WebView2, written from the exe (and checked
    against its SHA-256) the first time the Tempo window opens.
  - `tempo.log`: a line when something of Tempo went wrong, and at each start of watching the watched window's class
    and size. Never anything of a picture; it starts over when it passes 256 KB.
  - `logo.png`: the picture the notifications show.
- **Start with Windows**: the value `GameMarginsUploader` (the name it had before 0.2.5, kept so autostart stays one
  entry) under `HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Run`, only while **Start with Windows**
  on Settings is on. It is switched on at the first link; switching it off removes the value. While it is on, a start
  of the program points the value at the exe that runs, so a newer download takes over autostart, and an update
  points it at the new version.
- **A Start-menu shortcut**, `%APPDATA%\Microsoft\Windows\Start Menu\Programs\GameMargins Desktop.lnk`, pointing at
  the exe and carrying the program's AppUserModelID (`GameMargins.Uploader`). Windows needs it to show the program's
  notifications with its name and icon; it is rewritten when the exe moved or the program updated itself, and it makes
  the program findable in the Start menu. The shortcut of the old name, `GameMargins Uploader.lnk` in the same folder,
  is removed at start: that file only, and only when it points at a GameMargins exe.
- **The addon**, only while **Keep the addon up to date** is on and WoW is closed: `Interface\AddOns\GameMargins`
  in your WoW: Forever folder. A new version is unpacked in `Interface\GameMargins-staging` first (removed afterwards);
  the old folder moves to `Interface\GameMargins-backup\GameMargins-<old version>` (the last three are kept). Nothing
  else under `Interface\AddOns` is read or written.

Nothing is written in or beside MapleStory.

To remove it: switch **Start with Windows** off, choose **Quit**, unlink it under Account › Security, and delete the
exe, the Start-menu shortcut and `%LOCALAPPDATA%\GameMargins\Uploader`.

## Windows 10 and 11

- **WoW: Forever's side** needs 64-bit Windows with the .NET Framework 4.7.2 or later: Windows 11, and Windows 10
  from version 1803, have it built in.
- **Tempo needs Windows 10 version 1903 or later**, or Windows 11: that is where Windows began to let a program
  look at one window. On an older Windows 10, MapleStory Classic's page says "Tempo can't look at the game on this
  Windows", no question is asked and nothing of Tempo opens; WoW: Forever works as usual.
- **Windows 10 draws its own thin yellow border around a watched window** while Tempo looks, on every version of
  Windows 10; a program can't remove it there. On Windows 11 nothing is drawn around the game.
- **The mouse pointer** is left out of the picture Tempo reads on Windows 11 and on Windows 10 from version 2004. On
  Windows 10 versions 1903 and 1909 it can't be left out.
- **The Tempo window needs Microsoft's WebView2**, which is part of Windows 11 and of an updated Windows 10. When it
  is missing, MapleStory Classic's page says so and offers Microsoft's download; WoW: Forever works without it.

Each of these is asked of Windows before it is used. Tempo's side was written and tried on Windows 11; on Windows 10
it follows Microsoft's documentation, so please tell us (see "Reporting a problem") if something there is not as
written here.

## Building

On Windows 10 or 11, with nothing to install (the C# compiler of the .NET Framework that Windows ships):

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File build.ps1
```

Output: `dist\GameMarginsDesktop.exe`, and the same file as `dist\GameMarginsDesktop-<version>.exe`, the name a
release publishes; the build prints its SHA-256. It is one file of about 1.4 MB, built for 64-bit Windows. The version
is `Program.Version` in `src\Program.cs`, the one place it is written: `src\AssemblyInfo.cs` stamps it into the exe's
file data (Properties › Details), and the build checks that. The sources keep to C# 5, which that compiler speaks.

`lib\webview2` holds three files of Microsoft's WebView2 SDK (the NuGet package Microsoft.Web.WebView2, the version
its `README.md` names): `Microsoft.Web.WebView2.Core.dll`, `Microsoft.Web.WebView2.WinForms.dll` and the 64-bit
`WebView2Loader.dll`, with Microsoft's `LICENSE.txt` and `NOTICE.txt`. `build.ps1` refuses to build unless each has
the SHA-256 it pins, and embeds all three in the exe, so the program stays one file. They are Microsoft's, under
Microsoft's licence; this repository's `LICENSE` covers the rest.

The release builds are not code-signed, so Windows SmartScreen warns on the first start ("Windows protected your PC"
→ More info → Run anyway). Compare the file's SHA-256 with the one on the release page if in doubt.

Checks that need no game and no site:

```powershell
dist\GameMarginsDesktop.exe --shots C:\some\folder   # every screen, the float and the menu as PNGs, dark and light, at 100% and 150%; no window at all
dist\GameMarginsDesktop.exe --tempo-selftest          # looks at a window of its own; exit code, and %TEMP%\GameMargins-tempo-selftest.txt
```

Command-line options, for testing:

- `--site http://localhost:3000` points one start at another copy of the site (never remembered; a token from
  another site is dropped). The environment variable `GAMEMARGINS_SITE` does
  the same. Such a start never updates itself, and Tempo only talks to gamemargins.com or to a site on this computer
  (localhost or 127.0.0.1).
- `--wow C:\path\to\_forever_` points it at another WoW folder (remembered as the program's folder).
- `--data C:\some\folder` keeps everything the program remembers in that folder, so a test can run next to an
  installed copy: its own settings, no Start-menu shortcut, no autostart entry, and it never hands over to a newer
  version installed in the usual folder. Use it together with `--site`: against gamemargins.com itself such a start
  still updates itself.
- `--tempo-test-window "exact title"` makes Tempo watch the window with exactly that title in place of the game's;
  `--tempo-test-target <seconds>` shows a coloured window to watch for that long; `--tempo-open` (only with `--data`
  and a site on this computer) opens Tempo by itself once linked.
- `--test-toast` shows one test notification and writes the result to `%TEMP%\GameMargins-toast-test.txt`.
- `--updated-from <exe>` is what an update passes to the new version it starts (the exe it replaces), and
  `--update-failed <version>` what a version passes to itself when the update to that version did not stay up.

## Reporting a problem

Open an [issue](https://github.com/gamemargins/desktop/issues) with the program's version (at the foot of the
window's list, or in the exe's Properties › Details), your Windows version, and what the window or the notification
said. Never post your `settings.json` or a token. For anything about your account, or a security problem you would
rather not report in public, use the [contact page](https://gamemargins.com/contact).

## License

BSD 2-Clause, see `LICENSE`.

The license covers the code. The GameMargins name and logo (including `GameMargins.ico`) are not covered by it: please
give a modified build its own name and icon. The files in `lib\webview2` are Microsoft's, under the licence beside
them.
