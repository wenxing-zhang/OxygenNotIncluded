using System;
using System.Collections;
using System.Collections.Generic;
using HarmonyLib;
using Klei.AI;
using UnityEngine;

namespace InvincibleMinions
{
    // ---------- L2 疾病闸门：感染概率归零 ----------
    [HarmonyPatch(typeof(GermExposureMonitor), "GetContractionChance")]
    public static class Patch_GermExposure_GetContractionChance
    {
        public static bool Prefix(ref float __result)
        {
            __result = 0f;
            return false;
        }
    }

    // ---------- C6 烫伤 / 冻伤伤害归零 ----------
    [HarmonyPatch(typeof(ScaldingMonitor), "TakeScaldDamage")]
    public static class Patch_Scalding_TakeScaldDamage
    {
        public static bool Prefix() { return false; }
    }

    [HarmonyPatch(typeof(ScaldingMonitor), "TakeColdDamage")]
    public static class Patch_Scalding_TakeColdDamage
    {
        public static bool Prefix() { return false; }
    }

    // ---------- C2 窒息：直接否决缺氧判定（SuffocationMonitor 是状态机，改不了字段）----------
    [HarmonyPatch(typeof(OxygenBreather), "get_IsOutOfOxygen")]
    public static class Patch_IsOutOfOxygen
    {
        public static bool Prefix(ref bool __result)
        {
            __result = false;
            return false;
        }
    }

    [HarmonyPatch(typeof(OxygenBreather), "get_HasOxygen")]
    public static class Patch_HasOxygen
    {
        public static bool Prefix(ref bool __result)
        {
            __result = true;
            return false;
        }
    }

    // ---------- C3 冻伤：冷阈值的 static 字段改不了，改它的取值方法 ----------
    [HarmonyPatch(typeof(ExternalTemperatureMonitor), "GetExternalColdThreshold")]
    public static class Patch_GetExternalColdThreshold
    {
        public static bool Prefix(ref float __result)
        {
            __result = -273f;
            return false;
        }
    }

    // ---------- C/D 组运行期处理 ----------
    public static class EnvironmentRuntime
    {
        private static bool logged;
        private static bool loggedContainer;

        public static void Apply(GameObject go)
        {
            if (go == null) return;

            // C1 缺氧阈值：0 = 永远不算缺氧
            try
            {
                OxygenBreather ob = TraitRuntime.Find<OxygenBreather>(go);
                if (ob != null)
                {
                    ob.noOxygenThreshold = 0f;
                    ob.lowOxygenThreshold = 0f;
                }
            }
            catch (Exception e) { Warn("oxygen", e); }

            // C2 窒息：SuffocationMonitor 是 StateMachine 而非组件（编译器确认），
            // 无法直接改 timeUntilDeath。窒息由 Breath 数值条拉满 + OxygenBreather
            // 阈值归零共同覆盖。

            // C3 冷热阈值是 static readonly（编译器确认），无法赋值。
            // 温度免疫改由两处覆盖：
            //   - Database.Attributes.ScaldingThreshold / ScoldingThreshold = 1000000（AttributeEnhancer）
            //   - TakeScaldDamage / TakeColdDamage 被 Harmony 拦截
        }

        // D2/D4 治疗已感染的疾病（辐射病等）
        public static void CureAll(GameObject go)
        {
            if (go == null) return;
            try
            {
                Modifiers mods = TraitRuntime.Find<Modifiers>(go);
                if (mods == null) return;
                object container = mods.sicknesses;
                if (container == null) return;
                if (!loggedContainer)
                {
                    loggedContainer = true;
                    Debug.Log("[InvincibleMinions] sicknesses container = " + container.GetType().FullName);
                }

                // 容器可能是 List<Sickness>、Sickness 单实例，或 Sicknesses 组件
                IEnumerable e = container as IEnumerable;
                if (e == null)
                {
                    Klei.AI.Sickness single = container as Klei.AI.Sickness;
                    if (single != null)
                    {
                        try { single.Cure(go, null); Debug.Log("[InvincibleMinions] cured single sickness " + SafeId(single)); }
                        catch (Exception ex) { Warn("cure single", ex); }
                    }
                    return;
                }

                List<Klei.AI.Sickness> list = new List<Klei.AI.Sickness>();
                foreach (object o in e)
                {
                    Klei.AI.Sickness s = o as Klei.AI.Sickness;
                    if (s != null) list.Add(s);
                }

                for (int i = 0; i < list.Count; i++)
                {
                    try
                    {
                        list[i].Cure(go, null);
                        Debug.Log("[InvincibleMinions] cured sickness " + SafeId(list[i]) + " on " + go.name);
                    }
                    catch (Exception ex) { Warn("cure " + SafeId(list[i]), ex); }
                }
            }
            catch (Exception e) { Warn("cureAll", e); }
        }

        private static string SafeId(Klei.AI.Sickness s)
        {
            try { return s == null ? "?" : s.Id; }
            catch { return "?"; }
        }

        private static void Warn(string tag, Exception e)
        {
            if (!logged)
            {
                logged = true;
                Debug.LogWarning("[InvincibleMinions] env " + tag + " failed: " + e.Message);
            }
        }
    }
}
