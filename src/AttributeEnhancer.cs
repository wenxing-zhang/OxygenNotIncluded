using System.Reflection;
using Klei.AI;
using UnityEngine;

namespace InvincibleMinions
{
    // R6: 属性增强。只给"越大越好"的属性加值；
    // "越小越好"的属性压到 0；结构性属性（槽位/隔热）不碰。
    public static class AttributeEnhancer
    {
        public const float Big = 1000000f;
        // 移动速度：由 Athletics 驱动。100 太保守（用户反馈不够快），
        // 但 1000000 会让小人一帧跨越多个格子（穿墙/寻路抖动），取中间值。
        public const float AthleticsValue = 10000f;

        // 越大越好
        private static readonly string[] Positive = new string[]
        {
            "Construction", "Digging", "Machinery", "Athletics", "Learning", "Cooking",
            "Caring", "Strength", "Art", "Botanist", "Ranching", "LifeSupport", "Toggle",
            "PowerTinker", "FarmTinker", "SpaceNavigation", "Immunity", "GermResistance",
            "Insulation", "Luminescence", "Decor", "FoodQuality", "GeneratorOutput",
            "MachinerySpeed", "RadiationResistance", "RadiationRecovery", "MaxUnderwaterTravelCost",
            "ToiletEfficiency", "TransitTubeTravelSpeed", "DiseaseCureSpeed", "DoctoredLevel",
            "CarryAmount", "QualityOfLife", "ScaldingThreshold", "ScoldingThreshold"
        };

        // 越小越好：设成 1000000 会反向坑（耗氧暴涨 / 期望值爆表）
        private static readonly string[] Negative = new string[]
        {
            "AirConsumptionRate", "DecorExpectation", "FoodExpectation", "QualityOfLifeExpectation"
        };

        // 结构性 / 语义不明确：完全不碰
        private static readonly string[] Skipped = new string[]
        {
            "BionicBoosterSlots", "BionicBatteryCountCapacity", "ThermalConductivityBarrier",
            "RoomTemperaturePreference"
        };

        private static readonly BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

        public static void Apply(GameObject go)
        {
            if (go == null) return;
            Modifiers mods = TraitRuntime.Find<Modifiers>(go);
            if (mods == null) return;
            Attributes attributes = mods.attributes;
            if (attributes == null) return;
            Db db = Db.Get();
            if (db == null) return;
            Database.Attributes defs = db.Attributes;
            if (defs == null) return;

            FieldInfo[] fields = typeof(Database.Attributes).GetFields(Flags);
            for (int i = 0; i < fields.Length; i++)
            {
                string name = fields[i].Name;
                float value;
                if (IsIn(Positive, name))
                {
                    value = name == "Athletics" ? AthleticsValue : Big;
                }
                else if (IsIn(Negative, name))
                {
                    value = 0f;
                }
                else
                {
                    continue;
                }

                Attribute attr = null;
                try { attr = fields[i].GetValue(defs) as Attribute; }
                catch { attr = null; }
                if (attr == null) continue;

                try
                {
                    AttributeInstance inst = attributes.Get(attr);
                    if (inst == null) continue;
                    inst.Add(new AttributeModifier(attr.Id, value, "Invincible"));
                }
                catch (System.Exception e)
                {
                    if (InvincibleMinion.Verbose)
                    {
                        Debug.Log("[InvincibleMinions] attr " + name + " failed: " + e.Message);
                    }
                }
            }
        }

        private static bool IsIn(string[] set, string name)
        {
            for (int i = 0; i < set.Length; i++)
            {
                if (set[i] == name) return true;
            }
            return false;
        }
    }
}
