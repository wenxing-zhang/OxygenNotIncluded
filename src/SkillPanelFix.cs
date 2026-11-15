using HarmonyLib;
using Klei.AI;

namespace InvincibleMinions
{
    // 技能界面闪退的根因假设：全技能掌握后「下一级所需经验」为 0，
    // 界面拿它做百分比/除法时产生非法值（无托管异常，进程直接被 Unity 杀掉）。
    // 这里把两个经验条边界的返回值夹到合法范围。
    //
    // U59 变更：CalculateNext/PreviousExperienceBar 的返回类型从 int 改成了 float。
    // 旧代码用 ref int 声明 __result，Harmony 在 PatchAll 阶段直接抛
    //   "Cannot assign method return type System.Single to __result type System.Int32"
    // 导致整个模组加载失败并被游戏标记崩溃禁用（Player.log: Failed to load mod ...disabling）。
    [HarmonyPatch(typeof(MinionResume), "CalculateNextExperienceBar")]
    public static class Patch_CalculateNextExperienceBar
    {
        public static void Postfix(ref float __result)
        {
            if (__result <= 0f) __result = 1f;
        }
    }

    [HarmonyPatch(typeof(MinionResume), "CalculatePreviousExperienceBar")]
    public static class Patch_CalculatePreviousExperienceBar
    {
        public static void Postfix(ref float __result)
        {
            if (__result < 0f) __result = 0f;
        }
    }
}
