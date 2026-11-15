using System;
using System.Collections.Generic;
using HarmonyLib;
using Klei.AI;
using UnityEngine;

namespace InvincibleMinions
{
    // R7 + R10：选人界面生成候选时，改动一律放在 GenerateStats 全部跑完之后。
    // v0.4 崩溃教训：在 GenerateAptitudes / GenerateTraits 的 Postfix 里立刻改，
    // 会让 GenerateStats 内部后续步骤（GenerateAttributes）按原数量做索引 → 越界。
    [HarmonyPatch(typeof(MinionStartingStats), "GenerateStats")]
    public static class Patch_MinionStartingStats_GenerateStats
    {
        public static void Postfix(MinionStartingStats __instance)
        {
            if (__instance == null) return;

            // 1) 剔除负面特质
            try
            {
                List<Trait> traits = __instance.Traits;
                if (traits != null)
                {
                    for (int i = traits.Count - 1; i >= 0; i--)
                    {
                        Trait t = traits[i];
                        if (t != null && !t.PositiveTrait)
                        {
                            traits.RemoveAt(i);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("[InvincibleMinions] trait filter failed: " + e.Message);
            }

            // 2) 所有兴趣 = 1000000
            try
            {
                List<Database.SkillGroup> groups = AptitudeEnhancer.GetSkillGroups();
                if (groups.Count == 0) return;
                if (__instance.skillAptitudes == null) return;
                __instance.skillAptitudes.Clear();
                for (int i = 0; i < groups.Count; i++)
                {
                    __instance.skillAptitudes[groups[i]] = AptitudeEnhancer.AptitudeValue;
                }
                Debug.Log("[InvincibleMinions] starter stats patched: traits=" + (__instance.Traits == null ? -1 : __instance.Traits.Count) + " aptitudes=" + __instance.skillAptitudes.Count);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[InvincibleMinions] aptitude fill failed: " + e.Message);
            }
        }
    }
}
