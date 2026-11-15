using System;
using System.Collections.Generic;
using HarmonyLib;
using Klei.AI;
using UnityEngine;

namespace InvincibleMinions
{
    // 已停用（v0.6）：在 MinionStartingStats.GenerateStats 的 Postfix 里删除 Traits 列表项，
    // 会让 MinionStartingStats 的 stressTrait / joyTrait / congenitaltrait 字段指向
    // 「已不在列表中的对象」，CharacterContainer.SetInfoText 随即抛 NullReferenceException，
    // 选人界面直接崩。
    //
    // 现在改为运行期处理（InvincibleMinion.Init → TraitRuntime.Apply），不碰 MinionStartingStats。
    public static class TraitRuntime
    {
        public static void Apply(GameObject go)
        {
            if (go == null) return;
            Traits traits = Find<Traits>(go);
            if (traits == null)
            {
                Debug.LogWarning("[InvincibleMinions] Traits component not found on " + go.name);
                return;
            }
            try
            {
                List<Trait> list = traits.TraitList;
                if (list == null) return;
                int removed = 0;
                for (int i = list.Count - 1; i >= 0; i--)
                {
                    Trait t = list[i];
                    if (t != null && !t.PositiveTrait)
                    {
                        traits.Remove(t);
                        removed++;
                    }
                }
                if (removed > 0)
                {
                    Debug.Log("[InvincibleMinions] removed " + removed + " negative trait(s) from " + go.name);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("[InvincibleMinions] trait runtime failed: " + e.Message);
            }
        }

        // 组件可能在父/子物体上，三级兜底
        public static T Find<T>(GameObject go) where T : Component
        {
            if (go == null) return null;
            T c = go.GetComponent<T>();
            if (c != null) return c;
            c = go.GetComponentInParent<T>();
            if (c != null) return c;
            return go.GetComponentInChildren<T>();
        }
    }
}
