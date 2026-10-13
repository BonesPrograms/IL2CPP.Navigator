# IL2CPP Navigator verifier — reproducible test runbook

> **Source of truth (repository snapshot `ce01d5e`):** `C:\Users\user\Desktop\C#Lab\IL2CPP.Navigator.Verifier\Plugin.cs` defines **37 active `Test(...)` cases**. `InjectedProbe.cs` is the native-injection fixture. This document specifies those 37 cases in execution order, not the outdated 40-case count in `README.md` and `docs\TEST_COVERAGE.md`. Archived game logs show 40- and 41-case runs of *different, earlier builds*. There is **no recorded in-game 37/37 run** here. A successful build is not a successful native test.

## For an AI redeploying these tests: contract and safety rules

1. Read this file, then read the current `Plugin.cs`, `InjectedProbe.cs`, `XQuinn.NativeVerification.csproj`, and `NavigatorSource\Runtime\ChatbotNavigatorPatches.cs` **before** changing or deploying anything. Treat source as authoritative if counts, names, or assertions have changed. The verifier embeds its own `NavigatorSource\` snapshot; it does **not** reference live production source. Explicitly compare/synchronize the snapshot when testing production changes and record which revision you tested.
2. Do **not** edit the game's `bones` folder, the production plugin, or either `.csproj` to add per-file `Compile` rules. No project changes are needed to execute this suite. Never copy both Navigator DLLs into the game: both can inject the same IL2CPP type names, and their BepInEx IDs declare an incompatibility.
3. Always close the game before moving plugin DLLs. Back up the production DLL outside `BepInEx\plugins`, and restore it only after the game is closed and the verifier DLL is removed. Do not copy test fixtures or `NavigatorSource` into the plugin directory: deploy the compiled verifier DLL only. Do not modify unrelated plugins.
4. Do not delete existing log files to obtain a clean run. Preserve them, note timestamps, and collect the current launch's `RUN`, `PASS`, `FAIL`, and final result from `LogOutput.log`. Never call an aborted run a pass. A missing result, a test skipped by the array guard, or a result from an earlier launch is **not** 37/37.
5. This is an **in-process startup integration test** against a specific installed Vietnam War IL2CPP/Unity/BepInEx runtime. `dotnet test` is not the runner; the BepInEx plugin's `Load()` executes tests on game launch. Most checks use `NavigatorCore.Interface(...)` directly rather than simulating a player entering chat. Current source does **not** install or exercise the chatbot Harmony patches: consult the historical section before making claims about chatbot routing.

## Paths, prerequisites, build, deployment, evidence

| Role | Path / requirement |
| --- | --- |
| Verifier root | `C:\Users\user\Desktop\C#Lab\IL2CPP.Navigator.Verifier` |
| Project | `C:\Users\user\Desktop\C#Lab\IL2CPP.Navigator.Verifier\XQuinn.NativeVerification.csproj` |
| Test implementation | `C:\Users\user\Desktop\C#Lab\IL2CPP.Navigator.Verifier\Plugin.cs` |
| Native fixture | `C:\Users\user\Desktop\C#Lab\IL2CPP.Navigator.Verifier\InjectedProbe.cs` |
| Embedded runtime | `C:\Users\user\Desktop\C#Lab\IL2CPP.Navigator.Verifier\NavigatorSource` |
| Default game | `C:\Program Files (x86)\Steam\steamapps\common\VietnamWar` |
| BepInEx plugins | `C:\Program Files (x86)\Steam\steamapps\common\VietnamWar\BepInEx\plugins` |
| Output DLL | `C:\Users\user\Desktop\C#Lab\IL2CPP.Navigator.Verifier\bin\Release\net6.0\IL2CPP.Navigator.Verifier.dll` |
| Test log | `C:\Program Files (x86)\Steam\steamapps\common\VietnamWar\BepInEx\LogOutput.log` |
| Navigator log | `C:\Program Files (x86)\Steam\steamapps\common\VietnamWar\BepInEx\NavigatorLog.log` — **not an automatic assertion of the current suite** |

Prerequisites: .NET SDK able to build `net6.0`; the game's BepInEx Unity IL2CPP installation, including `BepInEx\core\0Harmony.dll`, `Il2CppInterop.Runtime.dll`, and generated `BepInEx\interop\Assembly-CSharp.dll`, `UnityEngine.CoreModule.dll`, and `Il2Cppmscorlib.dll`. NuGet restore must be possible or already cached. The verifier expects the game-specific `Assembly-CSharp` metadata and curated Unity types. Missing *optional* Unity modules may warn; the registration assertion still requires at least 75 curated Unity types. The default install path is defined in the project; for a relocated game, pass `-p:VietnamWarDirectory=<actual game root>` (quote the whole argument in PowerShell).

**Safe operational sequence (PowerShell, with game closed):**

```powershell
$project = 'C:\Users\user\Desktop\C#Lab\IL2CPP.Navigator.Verifier\XQuinn.NativeVerification.csproj'
$game = 'C:\Program Files (x86)\Steam\steamapps\common\VietnamWar'
$plugins = Join-Path $game 'BepInEx\plugins'
$production = Join-Path $plugins 'IL2CPP.Navigator.dll'
$verifier = Join-Path $plugins 'IL2CPP.Navigator.Verifier.dll'
$built = 'C:\Users\user\Desktop\C#Lab\IL2CPP.Navigator.Verifier\bin\Release\net6.0\IL2CPP.Navigator.Verifier.dll'
$backup = 'C:\Users\user\Desktop\IL2CPP.Navigator.dll.pre-verifier-backup'

dotnet build $project -c Release "-p:VietnamWarDirectory=$game"
if ($LASTEXITCODE -ne 0) { throw 'Verifier build failed; do not deploy' }
if (-not (Test-Path $built)) { throw "Missing built DLL: $built" }
if (Get-Process -Name 'Vietnam War' -ErrorAction SilentlyContinue) {
    throw 'Game is running; close it before changing plugin DLLs'
}
if (Test-Path $verifier) { throw "Verifier already present; inspect it before overwriting: $verifier" }
if (Test-Path $backup) { throw "Backup already exists; inspect it before moving plugins: $backup" }
if (Test-Path $production) {
    Move-Item -LiteralPath $production -Destination $backup
}
Copy-Item -LiteralPath $built -Destination $verifier
```

Launch the game **normally through BepInEx**, wait until chainloader startup completes (or capture the failure), then inspect only messages belonging to this launch. Example filter:

```powershell
$log = 'C:\Program Files (x86)\Steam\steamapps\common\VietnamWar\BepInEx\LogOutput.log'
Select-String -LiteralPath $log -Pattern 'IL2CPP Navigator Verifier.*(RUN |PASS |FAIL |NATIVE VERIFICATION)' |
    ForEach-Object { $_.Line }
```

The test helper emits `RUN <name>` **before** executing each case, `PASS <name>` after a successful assertion, or `FAIL <name>` plus exception on failure. Because `Test` catches case failures, later cases can still run; inspect *every* failure, not only the last line. `RUN` with no matching `PASS`/`FAIL` may identify a native crash. Cases 16–29 below run **only when** case 15 resolves both `Types.Array<T>` overloads. Initialization failure logs `NATIVE VERIFICATION BLOCKED` and stops before the final result. The final line is calculated from tests **actually executed**; `0 failed` with fewer than 37 passed does not constitute full coverage. The expected full result for this source, **if all tests pass**, is `NATIVE VERIFICATION RESULT: 37 passed, 0 failed. Native coverage applies to this game/runtime only.`

When finished: close the game; remove `IL2CPP.Navigator.Verifier.dll` from `BepInEx\plugins`; restore the backed-up production DLL **only if** no production DLL already exists, checking before moving. For example:

```powershell
if (Get-Process -Name 'Vietnam War' -ErrorAction SilentlyContinue) { throw 'Close the game first' }
if (Test-Path $verifier) { Remove-Item -LiteralPath $verifier }
if ((Test-Path $backup) -and -not (Test-Path $production)) {
    Move-Item -LiteralPath $backup -Destination $production
}
```

Record project commit/hash, built DLL hash, game/BepInEx versions, game launch time, log location, number of `RUN`/`PASS`/`FAIL` entries and final summary. A known *historical* run's copied logs are `C:\Users\user\AppData\Local\Cline\Navigator-verifier-native-test-2026-10-08.log` (40/0) and `C:\Users\user\AppData\Local\Cline\Navigator-verifier-instance-test-2026-10-08.log` (41/0); neither is evidence for this 37-test revision.

## Test harness, syntax, and shared dependencies

`Plugin.Load()` registers types once, constructs `NavigatorCore`, and runs `Test(name, () => { ... })` sequentially. `Check(condition, message)` throws on false; `Throws<T>(action)` requires exactly a compatible exception; `ReadInt(input)` converts `core.Interface(input)` to a native `Int32`. `Map(type)` maps native methods to Navigator's `MethodKey`; `FindCall(type, name, parameterTypes...)` matches **native parameter types**, returning `new`/method name with a `:N` overload index where necessary. `ReturnedLines(output)` extracts lines after `Returned:` and before `Loaded Type:`, `Loaded Instance Type:`, or `Loaded Variable:`. Do not hardcode overload indices: use native reflection, `FindCall`, or a `?methods` inspection, and omit `:0`.

Navigator expression notation: `@Type` selects a type; `?methods` queries methods; `*expression` loads an instance; `+name` saves the current instance in a variable; `-name` removes it; `^Type` casts the loaded instance; `this` reads the current instance; `member = value` assigns a writable member. `Types.Array<int>` has distinct native overloads: packed (`params T[]`, accepting `1,2,3`) and sized (`int`, accepting length `4`). In the implementation, case 15 discovers `packed` and `sized` strings via `Map(TypeRegister.GetTypeOrThrow("Types"))`. They are **not** guaranteed to be spelled `Types.Array<int>` without an overload suffix; all dependent cases use those discovered strings.

Cases 1–2 are prerequisites for the rest. Case 15 controls the 14 guarded cases (16–29). Case 17 injects and caches `InjectedProbe`; case 18 requires it. Many later tests mutate `core`'s current type/instance; preserve this order, or explicitly reestablish context when extracting cases into another harness. Variable tests clean up in `finally` where necessary. Tests are not independent unit tests that can be arbitrarily parallelized.

## Active case-by-case specification (37 in source order)

For each entry, **setup** refers to operations required before the assertion, **action** describes the actual verifier code, and **pass** specifies the observable condition. The corresponding `Test("...")` block in `Plugin.cs` is the executable definition; these instructions allow an AI to reproduce or port the intent without guessing fixtures.

### Registration, generic helpers, and metadata (cases 1–15)

1. **Vietnam War and curated Unity type registration.** Goal: detect missing game metadata, Unity curation, or helper injection. Setup: BepInEx game process, uninitialized type registry. Action: `HostTypeRegistration.Register(Log.LogWarning)` and store `registration`. Pass: `GameTypeCount > 1000`, `UnityTypeCount >= 75`, `CachedTypeCount > 0`; registry contains `GameObject` and the privileged `TypeRegister` facade. Optional unavailable Unity module warnings do not alone fail this test. This must run first.
2. **Navigator and registry initialization.** Goal: verify usable parser/core and fundamental aliases. Action: instantiate `new NavigatorCore()`; resolve cached `Types` and `Decimal`. Pass: `Types` exists and `Decimal` is the exact native `Il2CppSystem.Decimal` type via `Il2CppRuntime.SameType`. If core stays `null`, log `NATIVE VERIFICATION BLOCKED` and stop; do not infer results for later tests.
3. **Duplicate retention and nested-type quiet skip.** Goal: distinguish duplicate simple-name keys from intentionally ignored nested types. Setup: case 1's result. Action: compare its `SkippedTypeCount` with `TypeRegister.SkippedTypes.Count`; find an `Object` duplicate with full name `UnityEngine.Object`; compare the `TypeRegister.SkippedTypeCount()` Navigator result. Find the first native nested type from `Assembly-CSharp`, pass it to `CacheTypesSkipDuplicates(new[] { nested }, fullname: false)`. Pass: the nested type exists, registry entry count is unchanged, and that call returns **zero** duplicate entries.
4. **Types.Of<int>.** Goal: native generic type resolution. Action: `core.Interface("Types.Of<int>()")`. Pass: resulting native type equals `Il2CppType.Of<int>()`.
5. **Types.Num default.** Goal: default value of native generic numeric helper. Action: evaluate `Types.Num<int>()`. Pass: native `Int32` is `0`.
6. **Types.Num values and cached call.** Goal: ensure cached invocation metadata does not cache an old argument. Action: call `Types.Num<int>(42)` followed by `Types.Num<int>(43)`. Pass: results are `42` and `43`, respectively.
7. **Native non-blittable value-type default.** Goal: safely obtain the default of a nontrivial native struct. Action: evaluate `Types.Num<KVP<int,String>>()`. Pass: `Il2CppRuntime.TypeOf(result).IsValueType` is true; the test does **not** compare the contents of the KVP.
8. **Decimal literal.** Goal: literal conversion to a native decimal generic argument/result. Action: `Types.Num<Decimal>(1.25)`. Pass: `TypeOf(result)` matches native `Il2CppSystem.Decimal`; numeric equality with `1.25` is **not** asserted.
9. **Num constraint remains enforced.** Goal: generic type constraint validation. Action: `Types.Num<String>()`. Pass: `ArgumentException` is thrown (a successful return fails).
10. **Enum OR.** Goal: parse combined native enum flags. Action: `Types.Enum<BindingFlags>(Public|Instance)`. Pass: `ReadBindingFlags` equals `BindingFlags.Public | BindingFlags.Instance`.
11. **String null.** Goal: preserve a native null input rather than materializing a string. Action: `Types.String(null)`. Pass: `core.Interface(...) == null`.
12. **String value.** Goal: preserve punctuation in a native string. Action: `Types.String("a,b")`. Pass: `Il2CppRuntime.ReadString(...) == "a,b"`.
13. **Generic metadata query.** Goal: report signatures of generic injected helpers. Action: `@Types`, then `?methods`; join returned text rows. Pass: text contains **both** `Num<T>` and `params T[] arr`. This tests textual metadata display, not execution of an injected `params` method on `InjectedProbe`.
14. **Generic member query.** Goal: expose a native enumerable of generic field metadata. Action: `Types.Fields<int>()`; call `Il2CppRuntime.TryEnumerate` on result. Pass: it returns true; no particular item count is required.
15. **Resolve both Array<T> overloads.** Goal: avoid guessing overload IDs. Action: map methods on cached `Types`; find native methods named `Array` with one generic type argument; construct `Types.` + `CallName(key, "int")`; distinguish the overload whose first parameter is native `int` (`sized`) from the other (`packed`). Pass: both strings non-null. If either is missing, fail this case and **skip cases 16–29**; a reduced final pass count is incomplete, even with zero failures.

### Guarded arrays and native injection (cases 16–29; require both `packed` and `sized`)

16. **Types.Array<int> into List<int>(IEnumerable<int>).** Goal: demonstrate a native packed array can satisfy a native enumerable constructor. Setup: case 15; registry must contain `List<int>`. Action: discover the `List<int>.new` overload accepting native `IEnumerable<int>` via `FindCall`, construct it with `packed + "(11,22,33)"`, require native `List<int>`. Pass: `Count == 3`, elements `11,22,33` in order. Do not substitute an arbitrary generic `Dictionary` constructor; AOT coverage is game-dependent.
17. **Register injected class and inspect fields and params.** Goal: prove the fixture is registered and expose the actual IL2CPP member surface. Action: `ClassInjector.RegisterTypeInIl2Cpp<InjectedProbe>()` **before** `Il2CppType.Of<InjectedProbe>()`; cache type as `NativeInjectedProbe`, then `@NativeInjectedProbe`. Pass: native `_fields` contains `Number`, `Label`, `Items` but **not** `ManagedOnly`; `Number` has native `int` field type; `Items` has native `List<int>` field type. CLR reflection sees `ParamArrayAttribute` on `SumParams(params int[])`; native reflection sees **zero** `SumParams` methods. The test logs `INJECTED PARAMS REFLECTION: managed=true; native method count=0; not injected`. This is a fixture/runtime observation, **not** a universal claim about every IL2CPP `params` signature.
18. **Injected instance constructor, methods, array and fields.** Goal: exercise a genuinely injected object, not just reflection metadata. Setup: case 17's registered alias; discover its no-arg `new` and the native list enumerable constructor. Action: load `*NativeInjectedProbe.<ctor>()`; invoke `Add(5)` and read `Number` (both `5`); invoke `ArrayLength(<packed>(1,2,3))` (`3`); assign `Number = 7`, invoke `Add(2)` (`9`); assign/read `Label = "native-label"`; assign `Items = List<int>.<IEnumerable-constructor>(<packed>(4,5))` and read native list (`4,5`); save `+native_injected_probe`, then call `native_injected_probe.Add(1)` (`10`); always remove variable. Pass: all expected native fields/method return values and saved state match. Fixture uses `Il2CppValueField<int>`, `Il2CppStringField`, and `Il2CppReferenceField<Il2CppSystem.Collections.Generic.List<int>>`, plus the required `IntPtr` and ClassInjector constructors; see `InjectedProbe.cs` rather than introducing another identically named injected type.
19. **Expanded params.** Goal: verify packed integer array values. Action: evaluate `packed + "(1,2,3)"`, require `Il2CppSystem.Array`, read every element. Pass: length `3`, contents exactly `1,2,3`.
20. **Single expanded params element.** Goal: distinguish a one-item packed call from a sized-array call. Action: `packed + "(7)"`. Pass: length `1` and element `7` (not a length-seven array).
21. **Empty params.** Goal: empty packed array behavior. Action: `packed + "()"`. Pass: native array length `0`.
22. **Explicit null params.** Goal: distinguish explicit null from empty params. Action: `packed + "(null)"`. Pass: `NullReferenceException`; do **not** change the expectation to an empty array.
23. **Sized array.** Goal: the other overload allocates by length. Action: `sized + "(4)"`. Pass: native array length `4`; no element values are asserted.
24. **String params preserve punctuation and display values.** Goal: parser must preserve commas and bangs within string elements, and the feed must format a native enumerable. Action: change discovered packed call's `<int>` to `<String>`, invoke with `("a,b","c!d")`; read both native elements. Separately call `CommandChatbot.Navigator.SafeInterface(input)` and parse `ReturnedLines`. Pass: elements are `a,b` and `c!d`, rendered **exactly** as `[0] a,b` and `[1] c!d`. Calling the feed directly is **not** evidence that the ChatBot Harmony patch ran.
25. **Variables and CastInstance.** Goal: save/reload a native array through Navigator state. Action: `*<packed>(1,2,3)`, `+native_smoke_array`, `^Array`, `*native_smoke_array`, then read `this`; remove variable. Pass: reloaded native array length `3`.
26. **Native array enumeration.** Goal: verify both native enumeration and human-readable output. Action: `packed + "(10,20)"`, run `TryEnumerate`, read values; separately call the feed's `SafeInterface` and parse `ReturnedLines`. Pass: enumeration `10,20`, feed output exactly `[0] 10` and `[1] 20`.
27. **Native constructor invocation.** Goal: create an ordinary IL2CPP object by Navigator syntax. Action: `Object.new()`. Pass: result's native type matches `Il2CppSystem.Object`.
28. **Native properties and nonpublic instance fields.** Goal: inspect instance members and reject assignment to a read-only property. Setup: cache native `Il2CppSystem.Version` as `NativeSmokeVersion`, locate its `(int,int)` constructor; select `@NativeSmokeVersion`. Pass: `_props` contains `Major`; `_fields` contains a nonstatic field whose name ends with `Major` (discover actual name instead of assuming it). Construct `(1,2)`; `Major` and discovered field both read `1`; assigning `Major = 9` throws `Exception`; `Major` stays `1`.
29. **Native method invocation.** Goal: invoke a method on a loaded native receiver. Action: `*Types.String("abc")`; discover native `string.Contains(string)` by `FindCall`, call it with `"b"`. Pass: native Boolean is true.

### Remaining native state, reflection, and error tests (cases 30–37)

30. **Variables as arguments and instance member receivers.** Goal: validate both ways to use a saved object. Action: load `*Types.String("a!b")`; `+native_smoke_text` must return `Added current instance as variable native_smoke_text.`; pass the variable into `Types.String(native_smoke_text)` (returns `a!b`); discover/call `native_smoke_text.<Contains>("!")` (true); read `native_smoke_text.Length` (`3`); reload `*native_smoke_text` and read `this` (`a!b`). Finally remove `-native_smoke_text`, requiring `native_smoke_text removed from variables.`
31. **Invoking methods through property and field members.** Goal: method receiver resolution through an object-valued property **and** its native backing field. Setup: cache native `Exception` as `NativeSmokeException`; find constructors `(string)` and `(string,Exception)`. Construct inner `"inner-message"`, save `+native_smoke_inner`; construct outer `("outer-message",native_smoke_inner)`. Pass: `InnerException` property and `_innerException` field exist, and both `InnerException.ToString()` and `_innerException.ToString()` include `inner-message`. Remove the variable in `finally`.
32. **Assigning to a writable instance property and field.** Goal: verify return value and persistence of writes on both a loaded object and an explicit saved variable. Setup: cache native `StringBuilder` as `NativeSmokeBuilder`; find `(string)` constructor and load `"abcdef"` (`Length == 6`). Action/pass: `Length = 3` returns `3`, reads `3`, `ToString()` is `abc`; `m_ChunkLength = 2` returns `2`, reads `2`, `Length` is `2`, `ToString()` is `ab`. Save `+native_smoke_builder`, load an unrelated string as current instance, then `native_smoke_builder.Length = 1` returns/reads `1`, saved builder string `a`; `native_smoke_builder.m_ChunkLength = 0` returns/reads `0`, saved builder string empty. Remove variable in `finally`. These are verifier-owned native objects, **not** live game-object mutation.
33. **Isolated native void return and feed.** Goal: a void native call must not leak a previous result or fabricate an object. Setup: resolve native `System.GC` from `mscorlib`, cache as `NativeSmokeGC`, locate `KeepAlive(Il2CppSystem.Object)`. Action: `NativeSmokeGC.<call>(null)` through core and an independently created `NavigationFeed(false)`. Pass: core result `null`, feed `ReturnedLines` exactly `null`.
34. **Method and value cache namespaces.** Goal: ensure the same textual cache key does not let a value member overwrite a method member. Setup: find native `string.Contains(string)` `MethodBase` and `string.Length` `PropertyInfo`. Flush the relevant static cache; insert the method with key `shared-cache-key`; confirm `typeCached` true, `valueCached` false; insert property using returned flags. Pass: subsequent `FromCache<MethodBase>` and `FromCache<MemberInfo>` yield original method/property pointers respectively. Flush cache in `finally`.
35. **Native field read.** Goal: static native field access. Action: evaluate `int.MaxValue`. Pass: native `Int32` equals managed `int.MaxValue`.
36. **Native reflection exception unwrapping.** Goal: show the underlying IL2CPP exception rather than a CLR reflection wrapper. Setup: discover native `int.Parse(string)` overload. Action: call with `"not-an-integer"`. Pass: catches `Il2CppException` with `FormatException` in message, and message does **not** start with `System.Reflection.TargetInvocationException`; returning normally or throwing another exception fails.
37. **Native dictionary enumeration.** Goal: cover native `IEnumerable` / enumerator / disposal without assuming AOT support for a closed generic dictionary constructor. Action: create native `Il2CppSystem.Collections.Hashtable`, add one entry with keys/values boxed using `Il2CppRuntime.AsIl2CppObject(1, Il2CppType.Of<int>())` and corresponding `2`; call `TryEnumerate`. Pass: true and sequence contains exactly **one** entry; the test does not assert its printable content.

### Minimal code patterns for porting a case

These patterns are illustrations; use `Plugin.cs` for complete assertions and the current overload discovery. The `Test` helper's exception handling is essential for reporting every case:

```csharp
void Test(string name, Action check)
{
    Log.LogInfo("RUN " + name);
    try { check(); passed++; Log.LogInfo("PASS " + name); }
    catch (Exception ex) { failed++; Log.LogError("FAIL " + name + "\n" + ex); }
}

Test("Native field read", () =>
    Check(ReadInt("int.MaxValue") == int.MaxValue, "wrong native constant"));
```

Native injected fixtures require both construction paths and native field wrappers, not just CLR fields:

```csharp
public sealed class InjectedProbe : Il2CppSystem.Object
{
    public Il2CppValueField<int> Number = null!;
    public Il2CppStringField Label = null!;
    public Il2CppReferenceField<Il2CppSystem.Collections.Generic.List<int>> Items = null!;
    public InjectedProbe(IntPtr pointer) : base(pointer) { }
    public InjectedProbe() : base(ClassInjector.DerivedConstructorPointer<InjectedProbe>())
        => ClassInjector.DerivedConstructorBody(this);
    public int Add(int value) { Number.Value += value; return Number.Value; }
    public int ArrayLength(Il2CppSystem.Array values) => values.Length;
}
// In Plugin.Load, once per process, before Il2CppType.Of<InjectedProbe>():
ClassInjector.RegisterTypeInIl2Cpp<InjectedProbe>();
TypeRegister.CacheType(Il2CppType.Of<InjectedProbe>(), "NativeInjectedProbe");
```

If writing a new case, never call `ClassInjector.RegisterTypeInIl2Cpp<InjectedProbe>()` a second time in the same process. When syncing the runtime snapshot, retain the intended game-specific aliases and test fixture registration and rerun the **whole native suite**, not only a build.

## Historical chatbot tests: NOT active in the current 37

The older `README.md`/`docs\TEST_COVERAGE.md` describe chatbot integration that this revision no longer registers in the verifier's `Plugin.Load()`. The Harmony patch classes still exist in `NavigatorSource\Runtime\ChatbotNavigatorPatches.cs` (`ChatbotStringSnatcher` prefixes `ChatBot.ChatInput`; `CommandChatbot` prefixes `CompareChatWords`), but their **presence in the assembly does not install them**. The historical source at `git -C 'C:\Users\user\Desktop\C#Lab\IL2CPP.Navigator.Verifier' show HEAD^:Plugin.cs` (commit `c484182`) includes three additional cases: `ChatBot Harmony patches install` (persist one feed, `new Harmony(ChatbotNavigatorHooks.HarmonyId).PatchAll(...)`, confirm patch owners on both methods); `Leading bang routes to Thor once` (strip first `!`, capture text, allow one original `CompareChatWords` call, reset flag); and `Non-leading bang routes to Navigator and log` (preserve `!` inside the string, suppress original comparison, render `a!b` from `Types.String("a!b")`, check `NavigatorLog.log`). That old source also **deletes** `NavigatorLog.log` at startup; do not copy that behavior into a redeployment without deliberately preserving user logs. Historical tests mix direct prefix calls with native calls in the archived logs and are not interchangeable with a real chat/UI end-to-end test.

An archived 40/0 log from October 8 contains `ChatBot Harmony patches install`, `Native ChatBot calls route a leading bang to Thor`, and `Native ChatBot calls route non-leading bang to Navigator and log`. A later 41/0 archived log additionally contains `Native ChatBot instance directly invokes patched CompareChatWords`. The **exact** native-call test bodies for those archived runs are not in current `Plugin.cs` or the inspected historical commit; do not claim to be able to reproduce them byte-for-byte from a log. Restore them only from a verified source revision or reimplement explicitly with native `ChatBot` fixtures and updated assertions. In particular, a different plugin's Harmony prefix may short-circuit the original or another prefix; control other plugins/patch owners and make patch order explicit. A BepInEx assembly reference is not a plugin load-order dependency; Harmony's owner ID, not the BepInEx GUID, is used by Harmony before/after ordering.

For *manual production* chatbot smoke coverage (outside the 37 automated tests), remove the verifier, restore only the production Navigator DLL, launch through BepInEx, submit `Types.String("a!b")` through real game chat, and inspect **the new run's** `BepInEx\NavigatorLog.log` for `Instruction :Types.String("a!b")`, `Returned:` and `a!b`. If testing Thor routing, submit a leading-`!` command and observe the original path without double execution. Do not interpret a direct `SafeInterface` call, an old log, or the source alone as proof of in-game Harmony behavior.

## Failure triage and completion criteria

- **Build failure:** check installed BepInEx/interop DLLs, restored NuGet packages, correct `VietnamWarDirectory`, and compiler errors; do not deploy an earlier output DLL after a failed build.
- **Plugin not loaded / blocked:** check BepInEx plugin inventory and incompatible production Navigator DLL. Check `LogOutput.log` for `NATIVE VERIFICATION BLOCKED` or IL2CPP injection failures before analyzing case assertions.
- **Array case fails or fewer than 37 `RUN` lines:** inspect `Map(Types)` metadata and guard in case 15; cases 16–29 are conditional. Verify `RUN`/`PASS` name sets against the current source, not the old 40/41 README.
- **Native process exits after a `RUN`:** the last `RUN` is only a lead; collect complete BepInEx/Unity crash logs and verify game/interop version before retrying. Native faults cannot necessarily be caught by `Test`.
- **Wrong `List<T>` / injected fixture behavior:** verify registration-before-`Il2CppType.Of`, native field wrapper types, constructor overload chosen by native parameter type, AOT availability, and the single-loaded-DLL rule.
- **Success:** 37 distinct `RUN` names, 37 matching `PASS` names, zero `FAIL`, final **37 passed, 0 failed**, no blocked state, and evidence from a fresh launch of the *current build*. This asserts native/runtime coverage listed above, **not** production plugin equivalence or live chatbot Harmony behavior. Document any optional module warnings or additional loaded plugins alongside the result.