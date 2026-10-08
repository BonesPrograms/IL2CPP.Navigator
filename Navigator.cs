using System;
using System.IO;
using BepInEx;
using HarmonyLib;
using XQuinn.IO;
using XQuinn.CPPRuntime;
using XQuinn.BepInEx;

namespace XQuinn.BepInEx
{

    [HarmonyPatch(typeof(ChatBot), "CompareChatWords")]
    internal static class Navigator
    {
        internal static readonly string s_path = Path.Combine(Paths.BepInExRootPath, "NavigatorLog.log");
        static readonly Logger s_logger = Logger.New(s_path, true);
        static readonly NavigationFeed s_feed = new(true);

        [HarmonyPrefix]
        [HarmonyBefore]
        static bool NavigatorHook()
        {
            ChatBot instance = ChatBot.Instance;
            string chat = instance.Chat;
            if (chat.Length >= 1 && chat[0] == '!')
            {
                instance.Chat = chat.Substring(1);
                return true;
            }

            string output = s_feed.SafeInterface(chat);
            s_logger.Log(output, 1, 1);
            return false;
        }
    }
}