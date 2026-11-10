// The exe's file data, which Windows shows under Properties > Details and which antivirus heuristics read first: the
// program's name, its maker, its version and the copyright. Without them Windows says "File version 0.0.0.0", two
// downloads side by side can't be told apart, and an exe with no name, company or version looks more suspicious to a
// scanner. csc writes these attributes into the exe's version resource next to the icon (AssemblyTitle is what
// Windows calls "File description"). The version is Program.Version, the one place it is written; build.ps1 reads the
// same constant for the download's file name and checks the stamped result.

using System.Reflection;
using GameMargins.Uploader;

[assembly: AssemblyTitle("GameMargins Uploader")]
[assembly: AssemblyProduct("GameMargins Uploader")]
[assembly: AssemblyCompany("GameMargins")]
[assembly: AssemblyDescription("Uploads the GameMargins addon's WoW: Forever auction house scans to gamemargins.com and keeps the addon up to date.")]
[assembly: AssemblyCopyright("© 2026 GameMargins")]
[assembly: AssemblyVersion(Program.Version + ".0")]
[assembly: AssemblyFileVersion(Program.Version + ".0")]
[assembly: AssemblyInformationalVersion(Program.Version)]
