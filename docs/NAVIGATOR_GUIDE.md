# Navigator instruction and logging guide

Navigator runs as an IL2CPP BepInEx plugin in Vietnam War. Enter instructions in the game's chatbot input. This guide describes the behavior of the source in this folder; names available in a particular game depend on the runtime type registry and method overloads.

## Chat routing and output

- A chat string **beginning** with `!` goes to the original game/Thor command handling; the `!` is removed. An exclamation mark inside a quoted string is ordinary text.
- Other chat strings are sent to a persistent `NavigationFeed.SafeInterface`. The feed preserves the loaded instance and variables across instructions.
- Results are written to the game's `BepInEx\NavigatorLog.log`, **not posted into chat**. Inspect `BepInEx\LogOutput.log` for BepInEx startup messages and registration warnings.
- Scalar results print the returned value's native `ToString()` representation, rather than an interop proxy's CLR class name. Native enumerables print rows such as `[0] 10` and `[1] 20`; an isolated void result prints `null`.
- Errors are caught and formatted by `SafeInterface`; an error is not proof the instruction succeeded. Read the log.

## Core instruction forms

| Instruction | Purpose / example |
| --- | --- |
| `Types.String("a!b")` | Invoke a method via a cached type; expect `a!b` in `Returned:`. |
| `@String` | Load a cached type without an instance (for member queries / static calls). |
| `*Types.String("abc")` | Invoke and load the returned IL2CPP object as the current instance. |
| `this` | Read the current loaded instance. |
| `Length` | Read a field or property of the loaded type/instance (e.g. after loading a string). |
| `+my_text` | Save the current instance in a named variable. |
| `my_text.Length` | Access a property on a saved variable. |
| `my_text.Contains("b")` | Call an instance method through a variable. Some overloads require a `:N` suffix; use the member query to find it. |
| `*my_text` | Load the saved variable as the current instance. |
| `-my_text` | Remove the named variable. |
| `^Object` | Cast the current instance to a compatible cached type. |
| `?vars` | List saved variables; `?methods`, `?fields`, `?props` query the loaded type. |
| `~instruction1;instruction2` | Run several instructions in one chain; nested chains are not supported. |

Type names are resolved through the registry. The host registers eligible `Assembly-CSharp` types, built-in aliases (for example `int`, `String`, `Object`, `Types`), and available curated Unity types. Duplicate simple names can collide: query the `TypeRegister` facade or use a distinct cache key when registering a new type. A generic method such as `Types.Array<int>(10,20)` can return an enumerable; the exact `Array<T>` overload may require `:N`, as documented by `?methods` or `Types.Methods<...>()`.

## Member invocation and writes

### Making an injected type Navigator-friendly

Use an IL2CPP proxy base such as `Il2CppSystem.Object`, and follow ClassInjector's two-constructor pattern: `public MyTool(IntPtr pointer) : base(pointer) { }` and `public MyTool() : base(ClassInjector.DerivedConstructorPointer<MyTool>()) { ClassInjector.DerivedConstructorBody(this); }`. Register the class via `ClassInjector.RegisterTypeInIl2Cpp<MyTool>()` **before** calling `Il2CppType.Of<MyTool>()`. Then cache its IL2CPP type using `XQuinn.Reflection.TypeRegister.CacheType(Il2CppType.Of<MyTool>(), "MyTool")` so Navigator can resolve `MyTool`. The verifier's working example is `C:\Users\user\Desktop\C#Lab\IL2CPP.Navigator.Verifier\InjectedProbe.cs` with registration in its `Plugin.cs`. Avoid injecting a second class with the same fully qualified name in another plugin.

For instance data that Navigator must inspect, declare an eligible field wrapper such as `Il2CppValueField<int> Number`, `Il2CppStringField Label`, or `Il2CppReferenceField<Il2CppSystem.Collections.Generic.List<int>> Items`, and access its native contents with `.Value`. Managed-only fields and collections can be used as implementation details but are not native-reflected fields. Use IL2CPP-compatible method signatures for callable methods. Prefer an explicit `Il2CppSystem.Array` parameter, supplied with `Types.Array<T>`, over a managed `params T[]` method: the verifier's injected `params int[]` method did not appear in IL2CPP reflection. Initialization of managed-only state must account for both the managed and native construction paths.

Once registered and cached, use `@MyTool` and `?methods` to inspect overloads and constructors, `*MyTool.new()` to construct and load an instance, and `?fields` to see its IL2CPP-visible fields. The test fixture also accepted `Items = List<int>.new:N(Types.Array:M<int>(4,5))` using the overload indices displayed by `?methods`; the actual `N` and `M` must be looked up, not guessed (and the `:N`/`:M` suffixes are omitted when the index is zero). This recipe was exercised in Vietnam War using the standalone verifier; it is not a guarantee for every game or interop version.

After loading an IL2CPP object, methods can be invoked directly (`ToString()`), through an object-valued property (`InnerException.ToString()`), through an object-valued field (`_innerException.ToString()`), or through a saved variable (`my_text.Contains("b")`). A variable can also be used as an argument to a method. The verifier includes in-game tests for these routes.

Assignments use `left = right` and require a writable field or property. For example, after loading an IL2CPP `StringBuilder` registered as `NativeSmokeBuilder` in the verifier, `Length = 3` sets its length and `m_ChunkLength = 2` writes its instance field. With a saved variable, `my_builder.Length = 1` and `my_builder.m_ChunkLength = 0` address that variable even if another instance is currently loaded. **The `NativeSmokeBuilder` alias is created by the verifier and is not promised in the production registry**; this example explains the syntax, not a default game type.

Read-only properties cannot be assigned; the native setter decides whether a property is writable. Assigning to members of nested **struct fields** is explicitly unsupported because of boxing. Writing fields, invoking methods, and setting properties in live game objects can change game state: inspect the target type and take care before writing.

## Where to look next

- `README.md`: build, installation, chatbot routing.
- `docs\IMPLEMENTATION_AND_VERIFICATION.md`: code map, verified behavior, limits.
- `Runtime\NavigatorCore.cs`: instruction dispatch and assignments.
- `Runtime\NavigationFeed.cs`: result formatting and enumerable display.
- `Runtime\NavigatorEngine\Types.cs`: injected `Types` helper methods.
- `Reflection\TypeRegister.cs`: cached type names and duplicate handling.