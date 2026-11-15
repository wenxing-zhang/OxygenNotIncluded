using HarmonyLib;
using Klei.AI;

namespace InvincibleMinions
{
    // ---------- 不睡觉 ----------
    // 血泪教训：不要拦 SleepChoreMonitor 的 Create*Chore。
    // 它们是 GameStateMachine.SetupChore 的 create_chore_callback，返回值会被立刻解引用；
    // Prefix 返回 false 让回调不执行 -> chore 为 null -> SetupChore 里 NullReferenceException
    // （实测：Exception in (Minion).SleepChoreMonitor.root.sleeponfloor）。
    // 正确做法：让 chore 正常创建，再在 InvincibleMinion 的周期里用 ChoreDriver.StopChore 取消。

    // Narcolepsy 是纯条件查询，拦它安全（不涉及 chore 创建）。
    [HarmonyPatch(typeof(Narcolepsy), "IsNarcolepsing")]
    public static class Patch_Narcolepsy_IsNarcolepsing
    {
        public static bool Prefix(ref bool __result) { __result = false; return false; }
    }

    // 仿生复制人的「碎片整理」也是纯条件查询，安全。
    [HarmonyPatch(typeof(BionicBedTimeMonitor), "CanGoToBedTime")]
    public static class Patch_BionicBedTime_CanGoToBedTime
    {
        public static bool Prefix(ref bool __result) { __result = false; return false; }
    }

    [HarmonyPatch(typeof(BionicBedTimeMonitor), "ScheduleIsInBedTime")]
    public static class Patch_BionicBedTime_ScheduleIsInBedTime
    {
        public static bool Prefix(ref bool __result) { __result = false; return false; }
    }

    [HarmonyPatch(typeof(BionicBedTimeModeChore), "IsBedTimeAllowed")]
    public static class Patch_BionicBedTime_IsBedTimeAllowed
    {
        public static bool Prefix(ref bool __result) { __result = false; return false; }
    }
}
