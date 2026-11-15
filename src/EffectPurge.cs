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
    // 直接跳过：不进入 Radsick 分级，也就不会有「致命辐射病」。
    [HarmonyPatch(typeof(RadiationMonitor), "CheckRadiationLevel")]
    public static class Patch_RadiationMonitor_CheckRadiationLevel
    {
        public static bool Prefix() { return false; }
    }

    // ---------- 负面 effect 兜底清理 ----------
    // 「极度明亮」= effect SUNLIGHT_BURNING（本地化 STRINGS.DUPLICANTS.MODIFIERS.SUNLIGHT_BURNING）。
    // U59 里没有 SunlightMonitor，它由别处 Effects.Add 施加，没有单点可拦；
    // 统一做法：每 0.5s 把 Effects 里 isBad == true 的 effect 全部摘掉
    // （覆盖极度明亮 / 致命辐射病 / 鼓膜破裂 等一切负面状态）。
    // Effects.effects 是 private List<Effect>，Effect.isBad 是 public 字段。
    public static class EffectPurge
    {
        private static readonly BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
        private static FieldInfo effectsField;
        private static bool tried;
        private static bool logged;
        private static bool loggedFail;

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
                    if (effectsField == null)
                    {
                        Debug.LogWarning("[InvincibleMinions] Effects.effects field not found, purge disabled");
                    }
                }
                if (effectsField == null) return;

                List<Effect> list = effectsField.GetValue(fx) as List<Effect>;
                if (list == null || list.Count == 0) return;

                int removed = 0;
                for (int i = list.Count - 1; i >= 0; i--)
                {
                    Effect e = list[i];
                    if (e == null || !e.isBad) continue;
                    try
                    {
                        fx.Remove(e);
                        removed++;
                    }
                    catch (Exception ex)
                    {
                        if (!loggedFail)
                        {
                            loggedFail = true;
                            Debug.LogWarning("[InvincibleMinions] effect remove failed: " + ex.Message);
                        }
                    }
                }

                if (removed > 0 && !logged)
                {
                    logged = true;
                    Debug.Log("[InvincibleMinions] purged " + removed + " bad effect(s) on " + go.name);
                }
            }
            catch (Exception e)
            {
                if (!loggedFail)
                {
                    loggedFail = true;
                    Debug.LogWarning("[InvincibleMinions] effect purge failed: " + e.Message);
                }
            }
        }
    }
}
