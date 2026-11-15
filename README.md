# 无敌小人（Invincible Minions）

《缺氧》(Oxygen Not Included) 模组：让全队复制人不会死、不会受伤、不会生病，并附带全技能 / 全属性 / 好特质。

> 适配：本机实测游戏版本（Unity 6000.3.5f2，`Assembly-CSharp.dll` 17,674 类型）。
> 已装 Spaced Out；Bionic DLC 未装时相关数值条自动跳过（字段为 `null` 守卫），装上后自动生效。

## 功能

| 能力 | 实现位置 | 说明 |
|---|---|---|
| 不受伤 / 不死 | `src/Patches.cs` | Harmony Prefix 拦 `Health.Damage` / `DoDamage` / `Kill` / `Incapacitate` |
| 不失能 | `src/InvincibleMinion.cs` | `Health.canBeIncapacitated = false` |
| 不饿 / 不窒息 / 不生病 / 不辐射 / 不上厕所 / 不睡觉 | `src/InvincibleMinion.cs` | 每 0.5s 直接写 `AmountInstance.value`（拉满或归零），并用 `Amounts.SetValue` 兜底 |
| 疾病不感染 | `src/Environment.cs` | `GermExposureMonitor.GetContractionChance` Prefix → 0 |
| 已感染的疾病被清除 | `src/Environment.cs` | 每 2 秒遍历 `Modifiers.sicknesses` → `Sickness.Cure(go, null)` |
| 烫伤 / 冻伤无效 | `src/Environment.cs` | `ScaldingMonitor.TakeScaldDamage` / `TakeColdDamage` Prefix → false；另加 `ScaldingThreshold`/`ScoldingThreshold` = 1000000 |
| 不缺氧 | `src/Environment.cs` | `OxygenBreather.noOxygenThreshold` / `lowOxygenThreshold` = 0 |
| 全技能 | `src/SkillEnhancer.cs` | 只取 Db **真实注册**的技能表（反射 `ResourceSet<Skill>.resources`），fail-closed |
| 全属性 | `src/AttributeEnhancer.cs` | 白名单 35 项 = 1000000（`Athletics` = 100）；黑名单 4 项 = 0；槽位类与隔热不碰 |
| 全兴趣 = 1000000 | `src/AptitudePatch.cs` | 运行期 `SetAptitude(HashedString, float)`；选人界面在 `GenerateStats` Postfix 填充 |
| 只有好特质 | `src/TraitEnhancer.cs` | `MinionStartingStats.GenerateStats` Postfix 删除 `PositiveTrait == false` |

## 构建

本机**没有** dotnet SDK 与 Visual Studio，只有 .NET Framework 自带的 `csc.exe`：

```bat
build.bat
```

它会：
1. 用 `C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe` 编译 `src\*.cs`
2. 引用游戏 `Managed` 目录下的 `Assembly-CSharp.dll`、`Assembly-CSharp-firstpass.dll`、`0Harmony.dll`、`UnityEngine.CoreModule.dll`
3. 使用 `/nostdlib+` + 游戏自带的 `mscorlib` / `netstandard` / `System.Runtime`（游戏是 Unity 6，netstandard 2.1）
4. 输出 `dist\InvincibleMinions.dll` 并复制两个 yaml

> 游戏路径写死在 `build.bat` 的 `GAME=` 变量里（默认 `F:\SteamLibrary\steamapps\common\OxygenNotIncluded`），换机器需要改这一行。

## 安装

把 `dist\` 里的三个文件复制到：

```
%USERPROFILE%\Documents\Klei\OxygenNotIncluded\mods\Local\InvincibleMinions\
```

然后在游戏内 **模组 → 勾选「无敌小人」→ 应用 → 完全退出并重启游戏**（ONI 只在启动时加载 dll）。

## 日志自检

启动后 `Player.log`（`%USERPROFILE%\AppData\LocalLow\Klei\Oxygen Not Included\Player.log`）应出现：

```
Loading MOD dll: InvincibleMinions.dll
[InvincibleMinions] Harmony patches applied
[InvincibleMinions] skill registry ids=38
[InvincibleMinions] skill groups=14
[InvincibleMinions] init <小人名> amounts=True insts=N
[InvincibleMinions] update loop alive on <小人名>
[InvincibleMinions] starter stats patched: traits=N aptitudes=14
```

## 已知限制与踩过的坑

1. **`Database.Amounts.X` 不是静态字段**，必须 `Db.Get().Amounts` 访问；且 `Db.Get().Amounts` 返回 `Database.Amounts`（定义集），不是 `Klei.AI.Amounts`（组件）——两个同名类。
2. **`Database.Skills` 的字段里有未注册进 Db 的废弃技能**（`Swimming1/2` 的 Id 是 `Swimming`）。把这种 id 写进 `MinionResume.GrantedSkillIDs` 会让 `MinionResume.HasPerk` 每帧抛 `NullReferenceException`（实测 19177 次刷屏崩溃）。因此只取 `ResourceSet<Skill>.resources`。
3. **选人界面的改动必须放在 `GenerateStats` Postfix**：在 `GenerateAptitudes` / `GenerateTraits` 的 Postfix 里立刻改数量，会让 `GenerateAttributes` 按原数量索引 → `IndexOutOfRangeException`。
4. **`Athletics` 设 1000000 会让移动速度异常**，故限制为 100。
5. **`ThermalConductivityBarrier`（隔热厚度）不设**：它是「装备隔热 + 自身 Insulation」的合成值，且不参与烫伤/寒冷判定（那由环境温度驱动），设大对"无敌"零收益。
6. **`AirConsumptionRate` / `DecorExpectation` / `FoodExpectation` / `QualityOfLifeExpectation` 必须设 0 而非 1000000**（越大越差）。
7. **兴趣 1000000 只影响学习速度**，技能已全掌握时无实际效果，纯显示。

## 状态

- 编译：`build.bat` 通过（csc，exit 0）
- 部署：`mods\Local\InvincibleMinions\` 与 `dist\` 内容一致
- **游戏内行为：未完整验证**（无自动化手段启动游戏，需人工确认）
