using HarmonyLib;
using Klei.AI;

namespace InvincibleMinions
{
    // 技能面板闪退的根因假设：全技能掌握后「下一级所需经验」为 0，
    // 界面拿它做百分比/除法时产生非法值（无托管异常，进程直接被 Unity 杀掉）。
    // 这里把两个经验条边界的返回值夹到合法范围。
    [HarmonyPatch(typeof(MinionResume), "CalculateNextExperienceBar")]
    public static class Patch_CalculateNextExperienceBar
    {
        public static void Postfix(ref int __result)
        {
            if (__result <= 0) __result = 1;
        }
    }

    [HarmonyPatch(typeof(MinionResume), "CalculatePreviousExperienceBar")]
    public static class Patch_CalculatePreviousExperienceBar
    {
        public static void Postfix(ref int __result)
        {
            if (__result < 0) __result = 0;
        }
    }
}
