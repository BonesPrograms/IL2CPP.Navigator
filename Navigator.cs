using System;
using System.IO;
using BepInEx;
using HarmonyLib;
using XQuinn.IO;
using XQuinn.CPPRuntime;
using XQuinn.BepInEx;
using System.Collections.Generic;
using System.Text;
using XQuinn.Extensions;

namespace XQuinn.CPPRuntime
{

    [HarmonyPatch(typeof(ChatBot), nameof(ChatBot.ChatInput))]
    public static class Navigator
    {

        public static void Clear() => s_feed._core.Clear();
        public static string ChainInstruct(params string[] args)
        {
            StringBuilder sb = new();
            sb.Append('~');
            sb.AppendMany(args, ";");
            return s_feed.SafeInterface(sb.ToString());
        }

        internal static readonly string s_path = Path.Combine(Paths.BepInExRootPath, "NavigatorLog.log");
        static readonly Logger s_logger = Logger.New(s_path, true);
        internal static readonly NavigationFeed s_feed = new(true);

        [HarmonyPrefix]
        static bool NavigatorHook(ref string chat)
        {
            int index = chat.IndexOf('!');
            if (index >= 0)
            {
                index = index + 1;
                if (index < chat.Length)
                    chat = chat.Substring(index);
                return true;
            }
            string output = s_feed.SafeInterface(chat);
            s_logger.Log(output, 1, 1);
            return false;
        }
    }
}