using HarmonyLib;
using Klei.AI;

namespace InvincibleMinions
{
    // ---------- 不睡觉 ----------
    // 之前只把 Stamina 拉满 + 移除 Sleep urge，挡不住「睡觉」：
    // 复制人的睡眠由 SleepChoreMonitor 的状态机创建 chore（日程到点 / 疲劳 / 昏倒），
    // 另有 Narcolepsy 特质会随机打瞌睡。这里把创建入口和特质判定一起按死。
    [HarmonyPatch(typeof(SleepChoreMonitor), "CreateSleepChore")]
    public static class Patch_Sleep_CreateSleepChore
    {
        public static bool Prefix() { return false; }
    }

    [HarmonyPatch(typeof(SleepChoreMonitor), "CreateExhaustedSleepChore")]
    public static class Patch_Sleep_CreateExhaustedSleepChore
    {
        public static bool Prefix() { return false; }
    }

    [HarmonyPatch(typeof(SleepChoreMonitor), "CreatePassingOutChore")]
    public static class Patch_Sleep_CreatePassingOutChore
    {
        public static bool Prefix() { return false; }
    }

    [HarmonyPatch(typeof(SleepChoreMonitor), "CreateSleepOnFloorChore")]
    public static class Patch_Sleep_CreateSleepOnFloorChore
    {
        public static bool Prefix() { return false; }
    }

    [HarmonyPatch(typeof(Narcolepsy), "IsNarcolepsing")]
    public static class Patch_Narcolepsy_IsNarcolepsing
    {
        public static bool Prefix(ref bool __result) { __result = false; return false; }
    }

    // 仿生复制人没有普通睡眠，睡觉时段会进入 BionicBedTimeModeChore（「碎片整理」）。
    // 它由 BionicBedTimeMonitor 驱动，和 SleepChoreMonitor 完全两套，必须单独拦。
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
