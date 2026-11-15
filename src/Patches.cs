using HarmonyLib;
using UnityEngine;

namespace InvincibleMinions
{
    // 只保护复制人。小动物 / 野生生物 / 其它带 Health 的物体照常受伤、照常死亡。
    // 之前四个 Prefix 无条件 return false，把全星球所有 Health 都变成了无敌
    // （实测：攻击哈奇等小动物不掉血）。
    internal static class HealthFilter
    {
        // 复制人的 Health 不在 MinionIdentity 同一节点上（在子物体），所以要向上找。
        public static bool IsDuplicant(Health health)
        {
            if (health == null) return false;
            GameObject go = health.gameObject;
            if (go.GetComponent<MinionIdentity>() != null) return true;
            if (go.GetComponentInParent<MinionIdentity>() != null) return true;
            return false;
        }
    }

    // L1: 伤害入口。Health.Damage 是 ONI 唯一的扣血入口，
    // 烫伤 / 辐射 / 伤口 / 坠落最终都走这里。只对复制人丢弃。
    [HarmonyPatch(typeof(Health), "Damage")]
    public static class Patch_Health_Damage
    {
        public static bool Prefix(Health __instance) { return !HealthFilter.IsDuplicant(__instance); }
    }

    [HarmonyPatch(typeof(Health), "DoDamage")]
    public static class Patch_Health_DoDamage
    {
        public static bool Prefix(Health __instance) { return !HealthFilter.IsDuplicant(__instance); }
    }

    // 直接击杀路径（调试工具 / 特殊死法）：小动物可以被正常击杀
    [HarmonyPatch(typeof(Health), "Kill")]
    public static class Patch_Health_Kill
    {
        public static bool Prefix(Health __instance) { return !HealthFilter.IsDuplicant(__instance); }
    }

    // 失能（倒地流血）不进入：只对复制人
    [HarmonyPatch(typeof(Health), "Incapacitate")]
    public static class Patch_Health_Incapacitate
    {
        public static bool Prefix(Health __instance) { return !HealthFilter.IsDuplicant(__instance); }
    }

    // 注入点：每个小人 spawn 时挂上周期组件（含存档里已存在的小人）
    [HarmonyPatch(typeof(MinionIdentity), "OnSpawn")]
    public static class Patch_MinionIdentity_OnSpawn
    {
        public static void Postfix(MinionIdentity __instance)
        {
            if (__instance == null) return;
            if (__instance.gameObject.GetComponent<InvincibleMinion>() == null)
            {
                __instance.gameObject.AddComponent<InvincibleMinion>();
            }
        }
    }
}
