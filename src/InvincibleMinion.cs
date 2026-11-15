using System;
using Klei.AI;
using UnityEngine;

namespace InvincibleMinions
{
    // L3: 每个小人身上跑一个 0.5s 周期重置。
    // 覆盖不走 Health.Damage 的死亡路径：饥饿 / 窒息 / 辐射病 / 压力崩溃。
    public class InvincibleMinion : KMonoBehaviour
    {
        public static bool Physiology = true;
        public static bool NoToilet = true;
        public static bool NoSleep = true;
        public static bool AllSkills = true;
        public static bool MaxAttributes = true;
        public static bool Verbose = false;

        private Amounts amounts;
        private Attributes attributes;
        private Health health;
        private ChoreConsumer consumer;
        private float timer;
        private bool ready;
        private bool loggedTick;
        private int lastInstanceCount;

        protected override void OnSpawn()
        {
            base.OnSpawn();
            Init();
        }

        private void Update()
        {
            // 运行时 AddComponent 的组件不一定会收到 OnSpawn，这里做惰性初始化
            if (!ready) Init();
            if (!ready) return;
            if (!loggedTick)
            {
                loggedTick = true;
                Debug.Log("[InvincibleMinions] update loop alive on " + name);
            }
            timer += Time.unscaledDeltaTime;
            if (timer < 0.5f) return;
            timer = 0f;
            ResetAll();
        }

        private void Init()
        {
            if (ready) return;
            try
            {
                // Amounts / Attributes 不是 Component，宿主是 Modifiers 组件的 public 字段
                Modifiers mods = TraitRuntime.Find<Modifiers>(gameObject);
                if (mods != null)
                {
                    amounts = mods.amounts;
                    attributes = mods.attributes;
                }
                health = TraitRuntime.Find<Health>(gameObject);
                consumer = TraitRuntime.Find<ChoreConsumer>(gameObject);
                if (health != null)
                {
                    health.canBeIncapacitated = false;
                }
                ResetAll();
            }
            catch (Exception e)
            {
                Debug.LogError("[InvincibleMinions] core init failed: " + e);
            }

            // 每个增强独立隔离，任何一个失败都不影响周期重置
            try { if (AllSkills) SkillEnhancer.Apply(gameObject); }
            catch (Exception e) { Debug.LogError("[InvincibleMinions] skills failed: " + e); }

            try { if (MaxAttributes) AttributeEnhancer.Apply(gameObject); }
            catch (Exception e) { Debug.LogError("[InvincibleMinions] attributes failed: " + e); }

            try { AptitudeEnhancer.Apply(gameObject); }
            catch (Exception e) { Debug.LogError("[InvincibleMinions] aptitudes failed: " + e); }

            try { TraitRuntime.Apply(gameObject); }
            catch (Exception e) { Debug.LogError("[InvincibleMinions] traits failed: " + e); }

            ready = true;
            Debug.Log("[InvincibleMinions] init " + name
                + " amounts=" + (amounts != null)
                + " attrs=" + (attributes != null)
                + " health=" + (health != null)
                + " comps=" + DescribeComponents());
            ProbeBladder();
        }

        // 一次性自检：证明 SetValue 真的能改数值
        private void ProbeBladder()
        {
            if (amounts == null) return;
            try
            {
                float before = amounts.GetValue("Bladder");
                amounts.SetValue("Bladder", 0f);
                float after = amounts.GetValue("Bladder");
                float cal = amounts.GetValue("Calories");
                Debug.Log("[InvincibleMinions] probe bladder " + before + " -> " + after + " | calories=" + cal);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[InvincibleMinions] probe failed: " + e.Message);
            }
        }

        private void ResetAll()
        {
            // 血量兜底（不依赖 Db 是否就绪）
            if (health != null && health.hitPoints < health.maxHitPoints)
            {
                health.hitPoints = health.maxHitPoints;
            }

            Db db = Db.Get();
            if (db == null) return;
            Database.Amounts defs = db.Amounts;

            if (amounts != null && defs != null)
            {
                if (Physiology)
                {
                    Fill(defs.Calories);
                    Fill(defs.Breath);
                    Fill(defs.ImmuneLevel);
                    Fill(defs.HitPoints);
                    // Bionic DLC 的数值条：未装 DLC 时这些 Amount 为 null，Fill 内部判空
                    Fill(defs.BionicInternalBattery);
                    Fill(defs.BionicOxygenTank);
                    Fill(defs.BionicOil);
                    Drain(defs.Toxicity);
                    Drain(defs.RadiationBalance);
                    Drain(defs.BionicGunk);
                }
                if (NoToilet) Drain(defs.Bladder);
                if (NoSleep) Fill(defs.Stamina);
                Drain(defs.Stress);
            }

            if (consumer != null && db.Urges != null)
            {
                if (NoToilet)
                {
                    consumer.RemoveUrge(db.Urges.Pee);
                }
                if (NoSleep)
                {
                    consumer.RemoveUrge(db.Urges.Sleep);
                }
            }
        }

        // 诊断：当前物体上有哪些组件（定位 Amounts 为什么拿不到）
        private string DescribeComponents()
        {
            try
            {
                Component[] all = GetComponents<Component>();
                string s = "";
                for (int i = 0; i < all.Length && i < 40; i++)
                {
                    if (all[i] != null) s += all[i].GetType().Name + "|";
                }
                return s;
            }
            catch (Exception e)
            {
                return "err:" + e.Message;
            }
        }

        // 1 = 拉满, -1 = 归零, 0 = 不处理
        private static int ModeOf(string id)
        {
            switch (id)
            {
                case "Calories":
                case "Breath":
                case "ImmuneLevel":
                case "HitPoints":
                case "BionicInternalBattery":
                case "BionicOxygenTank":
                case "BionicOil":
                    return Physiology ? 1 : 0;
                case "Stamina":
                    return NoSleep ? 1 : 0;
                case "Bladder":
                    return NoToilet ? -1 : 0;
                case "Stress":
                case "Toxicity":
                case "RadiationBalance":
                case "BionicGunk":
                    return -1;
            }
            return 0;
        }

        private void Fill(Amount amount)
        {
            if (amount == null) return;
            float max = MaxOf(amount);
            if (max <= 0f) return;
            amounts.SetValue(amount.Id, max);
        }

        private void Drain(Amount amount)
        {
            if (amount == null) return;
            amounts.SetValue(amount.Id, 0f);
        }

        private float MaxOf(Amount amount)
        {
            if (amount == null) return 0f;
            Klei.AI.Attribute maxAttr = amount.maxAttribute;
            if (maxAttr != null && attributes != null)
            {
                float v = attributes.GetValue(maxAttr.Id);
                if (v > 0f) return v;
            }
            return 0f;
        }
    }
}
