﻿using Economics.Skill.Model.Options;
using Economics.Skill.Model.Options.Projectile;
using Economics.Skill.Model.Options.Range;
using Newtonsoft.Json;

namespace Economics.Skill.Model;

public class SkillContext
{
    [JsonProperty("名称")]
    public string Name { get; set; } = string.Empty;

    [JsonProperty("喊话")]
    public string Broadcast { get; set; } = string.Empty;

    [JsonProperty("限制等级")]
    public List<string> LimitLevel { get; set; } = new();

    [JsonProperty("限制进度")]
    public List<string> LimitProgress { get; set; } = new();

    [JsonProperty("触发设置")]
    public SkillSparkOption SkillSpark { get; set; } = new();

    [JsonProperty("伤害敌怪")]
    public StrikeNpcOption StrikeNpc { get; set; } = new();

    [JsonProperty("范围命令")]
    public ExecCommandOption ExecCommand { get; set; } = new();

    [JsonProperty("治愈")]
    public HealPlayerHPOption HealPlayerHPOption { get; set; } = new();

    [JsonProperty("清理弹幕")]
    public ClearProjectileOption ClearProjectile { get; set; } = new();

    [JsonProperty("拉怪")]
    public PullNpcOption PullNpc { get; set; } = new();

    [JsonProperty("弹幕")]
    public List<ProjectileOption> Projectiles { get; set; } = new();

}
