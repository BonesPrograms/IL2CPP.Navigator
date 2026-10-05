using BepInEx;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using XQuinn.BepInEx.Chatbot;
using XQuinn.Runtime;

namespace XQuinn.BepInEx
{
    /// <summary>BepInEx host for IL2CPP Navigator and the Vietnam War chatbot integration.</summary>
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    [BepInIncompatibility("xquinn.navigator.native-verification")]
    public sealed class Plugin : BasePlugin
    {
        static Harmony? s_harmony;
        static bool s_loaded;

        public static bool IsLoaded => s_loaded;

        public static NavigationFeed Navigator => CommandChatbot.Navigator;

        public override void Load()
        {
            HostRegistrationResult registration = HostTypeRegistration.Register(Log.LogWarning);

            s_harmony = new Harmony(ChatbotNavigatorHooks.HarmonyId);
            s_harmony.PatchAll(typeof(Plugin).Assembly);
            s_loaded = true;

            Log.LogInfo(
                $"{MyPluginInfo.PLUGIN_NAME} {MyPluginInfo.PLUGIN_VERSION} loaded. " +
                $"Processed {registration.GameTypeCount} Vietnam War types and {registration.UnityTypeCount} curated Unity types; " +
                $"cached {registration.CachedTypeCount} and retained {registration.SkippedTypeCount} duplicate-key entries. " +
                $"Chatbot Navigator output: {CommandChatbot.LogPath}");
        }

        public static string Execute(string instruction) => Navigator.SafeInterface(instruction);
    }
}