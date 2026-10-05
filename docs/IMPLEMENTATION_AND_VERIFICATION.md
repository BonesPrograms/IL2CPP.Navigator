# Source map and validation notes

## Scope / provenance

The original Navigator source folder was accidentally deleted and recovered as a byte-for-byte checked copy from the Windows Recycle Bin. The project includes all recovered source, the original build script and the `.csproj`. The main project file lists the files actually compiled; the recovered `NetConsole`, `ObjectModel`, `Private`, `retired`, and other nonincluded files were preserved, not silently discarded. The C# source is **not** generated from a decompilation of the installed plugin.

The production DLL in `C:\Program Files (x86)\Steam\steamapps\common\VietnamWar\BepInEx\plugins\IL2CPP.Navigator.dll` had SHA-256 `D5BDB252BC9648621FEDB5D20465FCE1B789E73D3EFEA46D49D2DE70F8DE050E` when this source was recovered; a fresh Release build reproduced the same DLL hash. These hashes describe the packaged snapshot, not future builds.

## Architecture

- `Plugin.cs`: BepInEx IL2CPP entry point, host registration, Harmony installation, public `Execute` entry.
- `Runtime\HostTypeRegistration.cs`: registers game types, optional curated Unity types, and privileged injected helper types. Missing optional Unity assemblies warn rather than abort registration.
- `Runtime\ChatbotNavigatorPatches.cs`: chatbot patches; routes leading-bang commands back to the game, normal chat to one `NavigationFeed`, and logs output.
- `Runtime\NavigatorCore.cs`: stateful instruction dispatcher; current instance/type, variable bindings, invocation, queries, casts, and assignments.
- `Runtime\NavigationFeed.cs`: safe error handling, formatted output, and enumeration of native/managed results.
- `Runtime\NavigatorEngine\`: parsing, native invocation/reflection conversion, and display string conversion. `Il2CppRuntime.ToDisplayString` invokes `ToString` via reflection for boxed native value types, so boxed `10` prints `10` rather than a proxy name or corrupt value.
- `Reflection\`: runtime type registry and member maps. The injected `Runtime\NavigatorEngine\Types.cs` and `Runtime\NavigatorEngine\TypeRegisterBridge.cs` expose useful registry and reflection operations within IL2CPP.
- `LangInterp\`, `Parsing\`, `IO\`: lexical/syntax handling, compatibility parsing helpers, and managed logging respectively.

## Tested behavior

The independent verifier's last in-game run for this code revision reported **37 passed, 0 failed** in `BepInEx\LogOutput.log`. Its cases cover host/Unity registration, collision handling, chatbot/Thor routing and logging; native generic/value types; `params` and arrays (including string punctuation); native enumerable display `[0] 10`, `[1] 20`; variables as arguments and receivers; invoking through property and field members; writing instance fields and properties directly and through variables; an isolated `GC.KeepAlive(null)` void call yielding `null`; and native reflection/error handling. The scalar `Types.String("a!b")` was confirmed in `NavigatorLog.log` as `Returned:` followed by `a!b`.

The 37/37 run tested the **verifier plugin embedding this runtime** in this specific game/runtime. The public production DLL was built and deployed and matched the recovered project by hash; it was not itself a separate 37-case test harness. The verifier's project now owns a snapshot of the shared source in its `NavigatorSource` directory. If either project is edited, rebuild and retest the relevant DLL; the snapshot will not automatically sync.

## Build and safety limits

- Target framework: .NET 6; compile against the target game's BepInEx IL2CPP and generated interop references. `dotnet build IL2CPP.Navigator.csproj -c Release` is a build only, not a deployment.
- The restored Release project builds successfully. A clean rebuild can report five existing compiler warnings (nullable fields/results, nonexhaustive lexer switch, unused `Preload.arr`); there were no build errors. Warnings should not be mistaken for a failed build.
- Never load production and verifier DLLs at the same time; both inject types and patch chatbot methods.
- Runtime registration depends on assemblies available in the installed game. Optional Unity modules may be absent; a count observed in one game is not portable to other games.
- This package does not contain a verbatim historical chat transcript or external documentation that was never supplied. These docs describe the recovered source and observed verification, not a guarantee for other IL2CPP games.