using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
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
        public static bool AllSkills = true;    // 用户实测：加入兴趣改动前一直正常
        public static bool MaxAttributes = true;
        public static bool Verbose = true;

        private Amounts amounts;
        private Attributes attributes;
        private Health health;
        private ChoreConsumer consumer;
        private float timer;
        private bool ready;
        private bool loggedTick;
        private bool loggedSetFail;
        private int tick;
        // 记录 Amounts 里不存在的条（例如未装 Bionic DLC 的条），只试一次
        private readonly HashSet<string> failedIds = new HashSet<string>();

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

            try { EnvironmentRuntime.Apply(gameObject); }
            catch (Exception e) { Debug.LogError("[InvincibleMinions] environment failed: " + e); }

            try { EnvironmentRuntime.CureAll(gameObject); }
            catch (Exception e) { Debug.LogError("[InvincibleMinions] cure failed: " + e); }

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
            try
            {
                if (health != null && health.hitPoints < health.maxHitPoints)
                {
                    health.hitPoints = health.maxHitPoints;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("[InvincibleMinions] health reset failed: " + e.Message);
            }

            // 路径 A 已移除：U59 里 AmountInstance 是普通 class（无基类、不是 Component），
            // GetComponentsInChildren<AmountInstance>() 永远返回 0；Amounts 组件本身也没有
            // 可反射的容器字段（元数据 FieldList 为空）。数值维持只走下面的路径 B（Amounts.SetValue）。

            // 路径 B：Amounts.SetValue（每条独立容错，单条失败不中断）
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
                try
                {
                    if (NoToilet) consumer.RemoveUrge(db.Urges.Pee);
                    if (NoSleep) consumer.RemoveUrge(db.Urges.Sleep);
                }
                catch (Exception e)
                {
                    if (Verbose) Debug.LogWarning("[InvincibleMinions] urge: " + e.Message);
                }
            }

            // 环境阈值每 tick 复写（可能被游戏状态机重置）；疾病治疗每 2 秒一次
            try { EnvironmentRuntime.Apply(gameObject); }
            catch { }
            tick++;
            // 负面 effect 每 tick 摘一遍（极度明亮 / 致命辐射病 / 鼓膜破裂 等）
            try { EffectPurge.Purge(gameObject); }
            catch { }
            if ((tick & 3) == 0)
            {
                try { EnvironmentRuntime.CureAll(gameObject); }
                catch { }
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
            SafeSet(amount, max);
        }

        private void Drain(Amount amount)
        {
            if (amount == null) return;
            SafeSet(amount, 0f);
        }

        // Amounts.SetValue 对未知 id 会直接抛 NRE（内部字典查不到）——单条独立容错
        private void SafeSet(Amount amount, float value)
        {
            if (amount == null || amounts == null) return;
            string id = amount.Id;
            if (string.IsNullOrEmpty(id) || failedIds.Contains(id)) return;
            try
            {
                amounts.SetValue(id, value);
            }
            catch (Exception e)
            {
                failedIds.Add(id);
                if (!loggedSetFail)
                {
                    loggedSetFail = true;
                    Debug.LogWarning("[InvincibleMinions] SetValue failed for " + id + " -> " + value + ": " + e.Message + " (will skip unknown ids from now on)");
                }
            }
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
