using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Klei.AI;
using UnityEngine;

namespace InvincibleMinions
{
    // R10: 所有兴趣（技能组）都存在，且等级 = 1000000。
    // 兴趣 = skillAptitudes：只影响学习速度，不影响已掌握技能。
    public static class AptitudeEnhancer
    {
        // 原版兴趣最高 +3。技能界面会拿这个值做等级渲染/索引，
        // 写 1000000 会让 UI 越界（实测：点技能界面直接崩，且无托管异常栈）。
        public const int AptitudeValue = 20;
        private static readonly BindingFlags Inst = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private static List<Database.SkillGroup> cachedGroups;

        // 从 ResourceSet<Database.SkillGroup>.resources 拿真实注册的技能组表（fail-closed）
        public static List<Database.SkillGroup> GetSkillGroups()
        {
            if (cachedGroups != null) return cachedGroups;
            List<Database.SkillGroup> groups = new List<Database.SkillGroup>();
            Db db = Db.Get();
            if (db == null) return groups;
            object set = db.SkillGroups;
            if (set == null) return groups;
            object res = null;
            Type t = set.GetType();
            int guard = 0;
            while (t != null && res == null && guard < 8)
            {
                FieldInfo f = t.GetField("resources", Inst);
                if (f != null)
                {
                    try { res = f.GetValue(set); }
                    catch { res = null; }
                }
                t = t.BaseType;
                guard++;
            }
            IEnumerable e = res as IEnumerable;
            if (e != null)
            {
                foreach (object o in e)
                {
                    Database.SkillGroup g = o as Database.SkillGroup;
                    if (g == null) continue;
                    // 已废弃 / 不可作为兴趣的组：其 choreGroupID 可能已本地化（如「游泳」），
                    // UI 拿它去 Db.ChoreGroups 查英文 id 会失败（Could not find ChoreGroup: 游泳）
                    if (!g.allowAsAptitude) continue;
                    groups.Add(g);
                }
            }
            cachedGroups = groups;
            Debug.Log("[InvincibleMinions] skill groups=" + groups.Count);
            for (int i = 0; i < groups.Count; i++)
            {
                Debug.Log("[InvincibleMinions] group " + groups[i].Id + " chore=" + groups[i].choreGroupID);
            }
            return groups;
        }

        // 运行期：给已存在的小人补设兴趣
        public static void Apply(GameObject go)
        {
            if (go == null) return;
            MinionResume resume = TraitRuntime.Find<MinionResume>(go);
            if (resume == null) return;
            List<Database.SkillGroup> groups = GetSkillGroups();
            if (groups.Count == 0)
            {
                Debug.LogWarning("[InvincibleMinions] skill groups unavailable, aptitudes skipped");
                return;
            }
            int ok = 0;
            for (int i = 0; i < groups.Count; i++)
            {
                try
                {
                    resume.SetAptitude(groups[i].Id, AptitudeValue);
                    ok++;
                }
                catch (Exception e)
                {
                    if (InvincibleMinion.Verbose)
                    {
                        Debug.Log("[InvincibleMinions] aptitude failed: " + e.Message);
                    }
                }
            }
            if (InvincibleMinion.Verbose)
            {
                Debug.Log("[InvincibleMinions] aptitudes applied=" + ok);
            }
        }
    }

    // 选人界面的兴趣填充已移到 TraitEnhancer.cs 的 GenerateStats Postfix，
    // 避免在 GenerateStats 内部步骤（GenerateAttributes）执行前改变数量导致越界。
}
