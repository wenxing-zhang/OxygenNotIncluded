using HarmonyLib;
using KMod;
using UnityEngine;

namespace InvincibleMinions
{
    public class InvincibleMinionsMod : UserMod2
    {
        public override void OnLoad(Harmony harmony)
        {
            // U59 的 KMod.UserMod2.OnLoad 内部已经执行 harmony.PatchAll(本程序集)。
            // 补丁在 base.OnLoad 里抛异常，就是「模组加载失败 / 被游戏标记崩溃并禁用」的直接原因
            // （Player.log: HarmonyException ... Failed to load mod ...disabling）。
            // 这里不再二次 PatchAll，避免同一补丁被应用两次。
            base.OnLoad(harmony);
            Debug.Log("[InvincibleMinions] Harmony patches applied");
        }
    }
}
