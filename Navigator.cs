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


    [HarmonyPatch(typeof(StartManager), "Start")]
    static class PreloadInit
    {
        [HarmonyPostfix]
        static void Init()
        {
            Navigator.s_feed._core.Clear();
            Navigator.RunPreloads();
        }

    }
    public interface IPreload
    {
        public string[] Chain { get; }
        public string Name { get; }
    }

    [HarmonyPatch(typeof(ChatBot), nameof(ChatBot.ChatInput))]
    public static class Navigator
    {

        readonly struct Preload
        {
            public readonly string InstructionChain;
            public readonly string Name;
            Preload(string arg, string name)
            {
                InstructionChain = arg;
                Name = name;
            }
            public static Preload New(IPreload preload)
            {
                string instruction = ChainInstruct(preload.Chain);
                return new Preload(instruction, preload.Name);
            }
            static string ChainInstruct(params string[] args)
            {
                StringBuilder sb = new();
                sb.Append('~');
                sb.AppendMany(args, ";");
                return sb.ToString();
            }
        }

        public static string ChainInstruct(params string[] args)
        {
            StringBuilder sb = new();
            sb.Append('~');
            sb.AppendMany(args, ";");
            return s_feed.SafeInterface(sb.ToString());
        }

        public static void Clear() => s_feed._core.Clear();

        internal static readonly string s_path = Path.Combine(Paths.BepInExRootPath, "NavigatorLog.log");
        static readonly Logger s_logger = Logger.New(s_path, true);
        internal static readonly NavigationFeed s_feed = new(true);
        static readonly List<Preload> s_preloads = new();
        public static void RegisterPreload<T>() where T : IPreload, new() => RegisterPreload(new T());
        public static void RegisterPreload(IPreload preload)
        {
            Preload copy = Preload.New(preload);
            s_preloads.Add(copy);
            s_logger.Log($"PRELOAD REGISTERED: {copy.Name} {copy.InstructionChain}");
        }

        internal static void RunPreloads()
        {
            StringBuilder sb = new();
            foreach (Preload preload in s_preloads)
            {
                sb.AppendLine();
                sb.Append("Running preload: ");
                sb.Append(preload.Name);
                sb.AppendLine();
                string returned = s_feed.SafeInterface(preload.InstructionChain);
                sb.AppendLine(returned);
            }
            s_logger.Log(sb.ToString(), 1, 1);
        }

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