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
    // 两轮实测得到的结论：
    //  1) 只按 Effect.isBad 过滤无效：日志 purged 出现 0 次；
    //  2) U59 里 Effects.effects 这个私有 List 可能是 null（懒初始化）——
    //     日志打出 "effects on 芙蕾娅 count=-1 -> "，说明连列表都拿不到。
    // 所以现在三条路并用：effects 列表（isBad）、GetAllEffectsForSerialization() 的 id 列表（黑名单）、
    // 以及首次运行把两边的完整内容打进日志，便于定位「极度明亮」到底挂在哪一层。
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

                List<Effects.SaveLoadEffect> saved = null;
                try { saved = fx.GetAllEffectsForSerialization(); }
                catch { saved = null; }

                if (!dumped)
                {
                    dumped = true;
                    string live = "";
                    if (list != null)
                    {
                        for (int i = 0; i < list.Count; i++)
                        {
                            Effect e = list[i];
                            if (e == null) continue;
                            live += SafeTag(e) + "(isBad=" + SafeIsBad(e) + ");";
                        }
                    }
                    string ids = "";
                    if (saved != null)
                    {
                        for (int i = 0; i < saved.Count; i++)
                        {
                            ids += saved[i].id + ";";
                        }
                    }
                    Debug.Log("[InvincibleMinions] effects on " + go.name
                        + " live=" + (list == null ? -1 : list.Count)
                        + " saved=" + (saved == null ? -1 : saved.Count)
                        + " liveIds[" + live + "] savedIds[" + ids + "]");
                }

                int removed = 0;

                if (list != null)
                {
                    for (int i = list.Count - 1; i >= 0; i--)
                    {
                        Effect e = list[i];
                        if (e == null || !IsBad(e)) continue;
                        try
                        {
                            fx.Remove(e);
                            removed++;
                        }
                        catch (Exception ex) { Warn("effect remove failed: " + ex.Message); }
                    }
                }

                if (saved != null)
                {
                    for (int i = 0; i < saved.Count; i++)
                    {
                        string id = saved[i].id;
                        if (!IsBadId(id)) continue;
                        try
                        {
                            fx.Remove(id);
                            removed++;
                        }
                        catch (Exception ex) { Warn("effect remove(id) failed: " + ex.Message); }
                    }
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
            return IsBadId(SafeTag(e));
        }

        private static bool IsBadId(string id)
        {
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
