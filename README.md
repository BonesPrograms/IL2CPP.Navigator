# IL2CPP Navigator — Vietnam War plugin

The complete recovered project source lives in this folder. `Plugin.cs` is the BepInEx entry point; the `IL2CPP.Navigator.csproj` declares exactly which source files and game references build the DLL. `Runtime`, `Reflection`, `LangInterp`, `Parsing`, and `IO` contain the implementation. Other recovered directories are retained for reference but are not all part of the build; consult the project file.

This plugin registers eligible `Assembly-CSharp` types and curated scene-facing Unity types. Missing optional Unity modules emit warnings, rather than aborting registration. Nested and compiler-generated types are skipped; duplicate simple-name collisions are recorded in `TypeRegister.SkippedTypes`. The managed `XQuinn.IO` logger writes chatbot output; it is not part of the IL2CPP runtime compatibility surface.

## Build and deploy

From PowerShell, with the game and BepInEx IL2CPP interop assemblies installed:

```powershell
dotnet build 'C:\Users\user\Desktop\C#Lab\VietnamWarModLab\IL2CPP.Navigator\IL2CPP.Navigator.csproj' -c Release
```

Output: `C:\Users\user\Desktop\C#Lab\VietnamWarModLab\IL2CPP.Navigator\bin\Release\net6.0\IL2CPP.Navigator.dll`. The included `build.ps1` runs the same build if local script execution is permitted. To build for a different game directory, pass `-p:VietnamWarDirectory=<path>` to `dotnet build` (keep that argument quoted if the path contains spaces). Copy **only this DLL**, not the verifier DLL, into `C:\Program Files (x86)\Steam\steamapps\common\VietnamWar\BepInEx\plugins` while the game is closed. Building does not deploy.

## Using the chatbot

Normal chat input is routed to one persistent Navigator feed. Leading `!` strips that marker and lets the original game/Thor command run once. Navigator output, including failures, is written to `C:\Program Files (x86)\Steam\steamapps\common\VietnamWar\BepInEx\NavigatorLog.log`; load and patch diagnostics are in `BepInEx\LogOutput.log`. The original `CompareChatWords` is suppressed for Navigator input. The plugin does **not** post Navigator's result back into chat; check the log.

Start with `Types.String("a!b")` and look for `Returned:` followed by `a!b`. Examples and instruction syntax are in `docs\NAVIGATOR_GUIDE.md`. Architecture, known limits, and verified behavior are in `docs\IMPLEMENTATION_AND_VERIFICATION.md`.

**Do not load this DLL together with `IL2CPP.Navigator.Verifier.dll`**: they inject the same types and patch the same chatbot methods. The verifier has its own README and local copy of the runtime source it tests.