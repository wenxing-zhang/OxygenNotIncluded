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
}
