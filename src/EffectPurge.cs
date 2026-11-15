using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Klei.AI;
using UnityEngine;

namespace InvincibleMinions
{
    // ---------- 致命辐射病 ----------
    // U59 里辐射病的唯一施加入口是 RadiationMonitor.CheckRadiationLevel
    // （private static void CheckRadiationLevel(Instance, float)）。
    [HarmonyPatch(typeof(RadiationMonitor), "CheckRadiationLevel")]
    public static class Patch_RadiationMonitor_CheckRadiationLevel
    {
        public static bool Prefix() { return false; }
    }

    // ---------- 负面 effect 清理 ----------
    // 教训（实测）：只按 Effect.isBad 过滤不够——「极度明亮」(SUNLIGHT_BURNING)
    // 在 U59 里 isBad 为 false，所以 previous 版本一行都没清掉（日志 purged 出现 0 次）。
    // 现在两条判据并用：isBad == true，或 effect 的 tag/id 命中黑名单。
    // 另外首次运行会把当前效果全部打进日志，便于确认它到底是不是 effect。
    public static class EffectPurge
    {
        private static readonly BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
        private static FieldInfo effectsField;
        private static bool tried;
        private static bool dumped;
        private static bool loggedFail;

        // 归一化（去下划线、转小写）后做子串匹配
        private static readonly string[] BadIdPatterns = new string[]
        {
            "sunlightburning", "sunburn",
            "radiationexposuredeadly", "radiationexposuresevere",
            "poppedeardrums", "eardrums"
        };

        public static void Purge(GameObject go)
        {
            if (go == null) return;
            try
            {
                Effects fx = TraitRuntime.Find<Effects>(go);
                if (fx == null) return;

                if (!tried)
                {
                    tried = true;
                    effectsField = typeof(Effects).GetField("effects", Flags);
                }

                List<Effect> list = null;
                if (effectsField != null)
                {
                    list = effectsField.GetValue(fx) as List<Effect>;
                }

                // 诊断：首次把当前 effect 全量打出来（id + isBad）
                if (!dumped)
                {
                    dumped = true;
                    string s = "";
                    if (list != null)
                    {
                        for (int i = 0; i < list.Count; i++)
                        {
                            Effect e = list[i];
                            if (e == null) continue;
                            s += SafeTag(e) + "(isBad=" + SafeIsBad(e) + ");";
                        }
                    }
                    Debug.Log("[InvincibleMinions] effects on " + go.name
                        + " count=" + (list == null ? -1 : list.Count) + " -> " + s);
                }

                if (list == null || list.Count == 0) return;

                int removed = 0;
                for (int i = list.Count - 1; i >= 0; i--)
                {
                    Effect e = list[i];
                    if (e == null) continue;
                    if (!IsBad(e)) continue;
                    try
                    {
                        fx.Remove(e);
                        removed++;
                    }
                    catch (Exception ex) { Warn("effect remove failed: " + ex.Message); }
                }
                if (removed > 0)
                {
                    Debug.Log("[InvincibleMinions] purged " + removed + " bad effect(s) on " + go.name);
                }
            }
            catch (Exception e) { Warn("effect purge failed: " + e.Message); }
        }

        private static bool IsBad(Effect e)
        {
            if (SafeIsBad(e)) return true;
            string id = SafeTag(e);
            if (string.IsNullOrEmpty(id)) return false;
            string norm = id.Replace("_", "").ToLowerInvariant();
            for (int i = 0; i < BadIdPatterns.Length; i++)
            {
                if (norm.Contains(BadIdPatterns[i])) return true;
            }
            return false;
        }

        private static string SafeTag(Effect e)
        {
            try { return e.tag.ToString(); }
            catch { return ""; }
        }

        private static bool SafeIsBad(Effect e)
        {
            try { return e.isBad; }
            catch { return false; }
        }

        private static void Warn(string msg)
        {
            if (!loggedFail)
            {
                loggedFail = true;
                Debug.LogWarning("[InvincibleMinions] " + msg);
            }
        }
    }
}
