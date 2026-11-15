using System.Reflection;
using HarmonyLib;
using KMod;
using UnityEngine;

namespace InvincibleMinions
{
    public class InvincibleMinionsMod : UserMod2
    {
        public override void OnLoad(Harmony harmony)
        {
            base.OnLoad(harmony);
            try
            {
                harmony.PatchAll(Assembly.GetExecutingAssembly());
                Debug.Log("[InvincibleMinions] Harmony patches applied");
            }
            catch (System.Exception e)
            {
                Debug.LogError("[InvincibleMinions] patch failed: " + e);
            }
        }
    }
}
