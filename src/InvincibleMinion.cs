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
                amounts = GetComponent<Amounts>();
                attributes = GetComponent<Attributes>();
                health = GetComponent<Health>();
                consumer = GetComponent<ChoreConsumer>();
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

            ready = true;
            Debug.Log("[InvincibleMinions] init " + name + " amounts=" + (amounts != null) + " insts=" + lastInstanceCount);
        }

        private void ResetAll()
        {
            // 血量兜底（不依赖 Db 是否就绪）
            if (health != null && health.hitPoints < health.maxHitPoints)
            {
                health.hitPoints = health.maxHitPoints;
            }

            // 路径 A：直接写 AmountInstance.value（不依赖 id 字典匹配）
            AmountInstance[] insts = null;
            try { insts = GetComponents<AmountInstance>(); }
            catch { insts = null; }
            if (insts != null)
            {
                lastInstanceCount = insts.Length;
                for (int i = 0; i < insts.Length; i++)
                {
                    AmountInstance inst = insts[i];
                    if (inst == null) continue;
                    string id = null;
                    try { if (inst.amount != null) id = inst.amount.Id; }
                    catch { id = null; }
                    if (string.IsNullOrEmpty(id)) continue;
                    int mode = ModeOf(id);
                    if (mode == 0) continue;
                    try
                    {
                        inst.value = mode > 0 ? inst.GetMax() : inst.GetMin();
                    }
                    catch { }
                }
            }

            // 路径 B：Amounts.SetValue 兜底（id 若与内部 key 不一致则跳过，不影响 A）
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
