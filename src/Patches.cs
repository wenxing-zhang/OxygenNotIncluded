using HarmonyLib;

namespace InvincibleMinions
{
    // L1: 所有伤害入口一律丢弃。Health.Damage 是 ONI 唯一的扣血入口，
    // 烫伤 / 辐射 / 伤口 / 坠落最终都走这里。
    [HarmonyPatch(typeof(Health), "Damage")]
    public static class Patch_Health_Damage
    {
        public static bool Prefix(Health __instance) { return false; }
    }

    [HarmonyPatch(typeof(Health), "DoDamage")]
    public static class Patch_Health_DoDamage
    {
        public static bool Prefix(Health __instance) { return false; }
    }

    // 直接击杀路径（调试工具 / 特殊死法）
    [HarmonyPatch(typeof(Health), "Kill")]
    public static class Patch_Health_Kill
    {
        public static bool Prefix(Health __instance) { return false; }
    }

    // 失能（倒地流血）不进入
    [HarmonyPatch(typeof(Health), "Incapacitate")]
    public static class Patch_Health_Incapacitate
    {
        public static bool Prefix(Health __instance) { return false; }
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
