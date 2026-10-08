using BepInEx;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using XQuinn.CPPRuntime;

namespace XQuinn.BepInEx
{

    public static class HarmonyInstance
    {
        public const string ID = "XQuinn.Navigator.Harmony";
        internal static readonly Harmony s_harmony = new Harmony(ID);
        internal static void Init()
        {
            s_harmony.PatchAll(typeof(Plugin).Assembly);
        }
    }

    /// <summary>BepInEx host for IL2CPP Navigator and the Vietnam War chatbot integration.</summary>
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    [BepInIncompatibility("xquinn.navigator.native-verification")]
    public sealed class Plugin : BasePlugin
    {

        public override void Load()
        {
            HostRegistrationResult registration = HostTypeRegistration.Register(Log.LogWarning);
            HarmonyInstance.Init();
            Log.LogInfo(
                $"{MyPluginInfo.PLUGIN_NAME} {MyPluginInfo.PLUGIN_VERSION} loaded. " +
                $"Processed {registration.GameTypeCount} Vietnam War types and {registration.UnityTypeCount} curated Unity types; " +
                $"cached {registration.CachedTypeCount} and retained {registration.SkippedTypeCount} duplicate-key entries. " +
                $"Chatbot Navigator output: {Navigator.s_path}");
        }
    }
}