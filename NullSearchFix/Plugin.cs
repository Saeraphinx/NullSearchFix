using HarmonyLib;
using IPA;
using IPA.Config;
using IPA.Config.Stores;
using IPA.Loader;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using IPALogger = IPA.Logging.Logger;


namespace NullSearchFix
{
    [Plugin(RuntimeOptions.SingleStartInit)]
    public class Plugin
    {
        internal static Plugin Instance { get; private set; }
        internal static IPALogger Log { get; private set; }
        internal static Harmony HarmonyInstance { get; private set; }
        public readonly string HarmonyID = "Saeraphinx.NullSearchFix";
        internal static Assembly Assembly { get; } = Assembly.GetExecutingAssembly();

        [Init]
        /// <summary>
        /// Called when the plugin is first loaded by IPA (either when the game starts or when the plugin is enabled if it starts disabled).
        /// [Init] methods that use a Constructor or called before regular methods like InitWithConfig.
        /// Only use [Init] with one Constructor.
        /// </summary>
        public void Init(IPALogger logger)
        {
            Instance = this;
            Log = logger;
            Log.Info("NullSearchFix initialized.");
        }

        [OnEnable]
        public void OnEnable()
        {
            HarmonyInstance = new Harmony(HarmonyID);
            ApplyHarmonyPatches();
        }

        [OnDisable]
        public void OnApplicationQuit()
        {
            RemoveHarmonyPatches();
        }

        internal static void ApplyHarmonyPatches()
        {
            try
            {
                Plugin.Log?.Debug("Applying Harmony patches.");
                HarmonyInstance.PatchAll(Assembly);
            }
            catch (Exception ex)
            {
                Plugin.Log?.Error("Error applying Harmony patches: " + ex.Message);
                Plugin.Log?.Debug(ex);
            }
        }
        internal static void RemoveHarmonyPatches()
        {
            try
            {
                // Removes all patches with this HarmonyId
                HarmonyInstance.UnpatchSelf();
            }
            catch (Exception ex)
            {
                Plugin.Log?.Error("Error removing Harmony patches: " + ex.Message);
                Plugin.Log?.Debug(ex);
            }
        }
    }
}

namespace NullSearchFix.Patches
{
    internal class Patch
    {
        [HarmonyPatch(typeof(AlphabetScrollbarInfoBeatmapLevelHelper))]
        [HarmonyPatch("CreateData")]
        internal class PatchCreateData
        {

            // Prefix: Before the regular method runs - bool disables method.
            private static bool Prefix(ref IReadOnlyList<AlphabetScrollInfo.Data> __result, IReadOnlyList<IPreviewBeatmapLevel> previewBeatmapLevels, bool sortPreviewBeatmapLevels, IReadOnlyList<IPreviewBeatmapLevel> sortedPreviewBeatmapLevels) {

                List<AlphabetScrollInfo.Data> list = new List<AlphabetScrollInfo.Data>();
                if (previewBeatmapLevels == null || previewBeatmapLevels.Count == 0)
                {
                    sortedPreviewBeatmapLevels = null;
                    __result = null;
                    return false;
                }

                if (sortPreviewBeatmapLevels)
                {
                    sortedPreviewBeatmapLevels = (from x in previewBeatmapLevels
                                                  orderby x.songName.ToUpperInvariant()
                                                  select x).ToArray<IPreviewBeatmapLevel>();
                    string text = "  ";
                    try
                    {
                        text = sortedPreviewBeatmapLevels[0].songName.ToUpperInvariant().Substring(0, 1); // ISSUE HERE
                    } catch
                    {
                         Plugin.Log?.Debug("NullSearchFix: Error getting first song name - Found zero length string.");
                    }
                    if (string.CompareOrdinal(text, "A") < 0)
                    {
                        list.Add(new AlphabetScrollInfo.Data('#', 0));
                    }
                    else
                    {
                        list.Add(new AlphabetScrollInfo.Data(text[0], 0));
                    }
                    for (int i = 1; i < sortedPreviewBeatmapLevels.Count; i++)
                    {
                        string text2 = "  ";
                        try
                        {
                            text2 = sortedPreviewBeatmapLevels[i].songName.ToUpperInvariant().Substring(0, 1); // ISSUE HERE
                        }
                        catch
                        {
                            Plugin.Log?.Debug("NullSearchFix: Error getting song name - Found zero length string.");
                        }
                        if (string.CompareOrdinal(text2, "A") >= 0 && text != text2)
                        {
                            text = text2;
                            if (list.Count >= 27)
                            {
                                list.Add(new AlphabetScrollInfo.Data(text2[0], i));
                                break;
                            }
                            list.Add(new AlphabetScrollInfo.Data(text2[0], i));
                        }
                    }
                }
                else
                {
                    sortedPreviewBeatmapLevels = previewBeatmapLevels;
                    __result = null;
                }
                __result = list;
                return false;
            }
        }
    }
}