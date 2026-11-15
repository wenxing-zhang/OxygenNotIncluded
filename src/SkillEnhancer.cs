using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Klei.AI;
using UnityEngine;

namespace InvincibleMinions
{
    // R5: 全技能。
    // 关键教训（v0.2 崩溃）：Database.Skills 的字段里存在「未注册进 Db」的 Skill
    // （例如 Swimming1/2 的 Id 是 Swimming，新版已废弃），把这种 id 写进
    // MinionResume.GrantedSkillIDs 会让 HasPerk 查 Db 失败并每帧抛 NRE。
    // 因此这里只遍历 Db 真实注册的资源表；拿不到就什么都不做（fail-closed）。
    public static class SkillEnhancer
    {
        private static readonly BindingFlags Inst = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private static List<string> cachedIds;
        private static bool tried;

        public static void Apply(GameObject go)
        {
            if (go == null) return;
            MinionResume resume = go.GetComponent<MinionResume>();
            if (resume == null) return;
            Db db = Db.Get();
            if (db == null) return;
            Database.Skills skills = db.Skills;
            if (skills == null) return;

            List<string> ids = GetRegisteredIds(skills);
            if (ids.Count == 0)
            {
                Debug.LogWarning("[InvincibleMinions] skill registry unavailable, all-skills skipped");
                return;
            }

            Cleanup(resume, ids);

            int ok = 0;
            int failed = 0;
            for (int i = 0; i < ids.Count; i++)
            {
                try
                {
                    resume.GrantSkill(ids[i]);
                    resume.MasterSkill(ids[i]);
                    ok++;
                }
                catch (System.Exception e)
                {
                    failed++;
                    if (InvincibleMinion.Verbose)
                    {
                        Debug.Log("[InvincibleMinions] skill " + ids[i] + " failed: " + e.Message);
                    }
                }
            }
            if (InvincibleMinion.Verbose)
            {
                Debug.Log("[InvincibleMinions] skills granted=" + ok + " failed=" + failed);
            }
        }

        // 从 ResourceSet<Skill>.resources 拿真实注册的技能表（沿继承链找字段）
        private static List<string> GetRegisteredIds(Database.Skills skills)
        {
            if (tried) return cachedIds;
            tried = true;
            List<string> ids = new List<string>();
            object res = null;
            Type t = skills.GetType();
            int guard = 0;
            while (t != null && res == null && guard < 8)
            {
                FieldInfo f = t.GetField("resources", Inst);
                if (f != null)
                {
                    try { res = f.GetValue(skills); }
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
                    Database.Skill s = o as Database.Skill;
                    if (s == null) continue;
                    if (string.IsNullOrEmpty(s.Id)) continue;
                    if (s.deprecated) continue;
                    ids.Add(s.Id);
                }
            }
            cachedIds = ids;
            Debug.Log("[InvincibleMinions] skill registry ids=" + ids.Count);
            return ids;
        }

        // 清掉存档里可能残留的无效 skill id（崩溃过的档救回来）
        private static void Cleanup(MinionResume resume, List<string> valid)
        {
            try
            {
                FieldInfo gf = typeof(MinionResume).GetField("GrantedSkillIDs", Inst);
                if (gf != null)
                {
                    object raw = gf.GetValue(resume);
                    IList list = raw as IList;
                    if (list != null)
                    {
                        for (int i = list.Count - 1; i >= 0; i--)
                        {
                            string id = list[i] as string;
                            if (id != null && !valid.Contains(id)) list.RemoveAt(i);
                        }
                    }
                    else if (raw != null)
                    {
                        // HashSet<string> 之类非 IList 容器：反射调 Remove(string)
                        MethodInfo remove = raw.GetType().GetMethod("Remove", new Type[] { typeof(string) });
                        IEnumerable items = raw as IEnumerable;
                        if (remove != null && items != null)
                        {
                            List<string> bad = new List<string>();
                            foreach (object o in items)
                            {
                                string id = o as string;
                                if (id != null && !valid.Contains(id)) bad.Add(id);
                            }
                            for (int i = 0; i < bad.Count; i++)
                            {
                                try { remove.Invoke(raw, new object[] { bad[i] }); }
                                catch { }
                            }
                        }
                    }
                }
                FieldInfo mf = typeof(MinionResume).GetField("MasteryBySkillID", Inst);
                if (mf != null)
                {
                    IDictionary dict = mf.GetValue(resume) as IDictionary;
                    if (dict != null)
                    {
                        List<object> bad = new List<object>();
                        foreach (object k in dict.Keys)
                        {
                            string id = k as string;
                            if (id != null && !valid.Contains(id)) bad.Add(k);
                        }
                        for (int i = 0; i < bad.Count; i++) dict.Remove(bad[i]);
                    }
                }
            }
            catch (System.Exception e)
            {
                if (InvincibleMinion.Verbose)
                {
                    Debug.Log("[InvincibleMinions] cleanup failed: " + e.Message);
                }
            }
        }
    }
}
