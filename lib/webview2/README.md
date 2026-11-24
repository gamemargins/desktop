# Microsoft WebView2 SDK files

Three files of the NuGet package **Microsoft.Web.WebView2 1.0.4258.31**, as Microsoft publishes it
(https://www.nuget.org/packages/Microsoft.Web.WebView2/1.0.4258.31; the package's SHA-256 is
`56f7f4b8bf9aee4b8efefbbdd4f67d5f74ebd1b100ed0806da71bf76af481aa9`), with its `LICENSE.txt` and `NOTICE.txt`:

| File | In the package | SHA-256 |
| --- | --- | --- |
| `Microsoft.Web.WebView2.Core.dll` | `lib/net462/` | `d60e6e94245078cb56e44ebd5f360dc04bf18e460136be93bfc4cfca43b98505` |
| `Microsoft.Web.WebView2.WinForms.dll` | `lib/net462/` | `6a9e6856f23c463783c3afb0ca5d78f56324c86657162ccb16d5227b3233b32a` |
| `WebView2Loader.dll` (x64) | `runtimes/win-x64/native/` | `3426dcc55fdfb8b5e7ac623cf33b4ccf283fbf68a0c5a65bddfdb4d749a8abee` |

`build.ps1` checks these hashes, references the two managed libraries and embeds all three in the exe, so the program
stays one file; `src/Embedded.cs` loads them from there. They are Microsoft's, under Microsoft's licence
(`LICENSE.txt`, which the exe carries too); the rest of this repository is under its own `LICENSE`.

A newer SDK: replace the three files and the two texts from the new package, and change the version and the hashes
here, in `build.ps1` and in `src/Embedded.cs` together.
