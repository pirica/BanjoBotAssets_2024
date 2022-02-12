﻿using Newtonsoft.Json;
using System.Diagnostics.CodeAnalysis;

namespace BanjoBotAssets.Artifacts
{
    [NamedItemData("Schematic")]
    internal class SchematicItemData : NamedItemData
    {
        public string? EvoType { get; set; }
        [DisallowNull]
        public AlterationSlot[]? AlterationSlots { get; set; }

        public RangedWeaponStats? RangedWeaponStats { get; set; }
        public MeleeWeaponStats? MeleeWeaponStats { get; set; }
        public TrapStats? TrapStats { get; set; }
    }

    /// <summary>
    /// Only included for ranged weapon schematics
    /// </summary>
    internal class RangedWeaponStats
    {
        public string? AmmoType { get; set; }
        public int? BulletsPerCartridge { get; set; }
        public float? FiringRate { get; set; }
        public DamageRange? PointBlank { get; set; }
        public DamageRange? MidRange { get; set; }
        public DamageRange? LongRange { get; set; }
        public DamageRange? MaxRange { get; set; }
        public int? Durability { get; set; }
        public float? DurabilityPerUse { get; set; }
        public float? BaseCritChance { get; set; }
        public float? BaseCritDamage { get; set; }
        public int? AmmoCostPerFire { get; set; }
        public float? KnockbackZAngle { get; set; }
        public float? StunTime { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public OverheatingInfo? Overheat { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public ReloadInfo? Reload { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public ChargeInfo? Charge { get; set; }
    }

    /// <summary>
    /// Only included if <see cref="ReloadTime"/> is nonzero
    /// </summary>
    internal struct ReloadInfo
    {
        public float ReloadTime { get; set; }
        public string ReloadType { get; set; }
        public int ClipSize { get; set; }
        public int InitialClips { get; set; }
        public int CartridgePerFire { get; set; }
    }

    /// <summary>
    /// Only included if <see cref="MaxChargeTime"/> is nonzero
    /// </summary>
    internal struct ChargeInfo
    {
        public float FullChargeDurabilityPerUse { get; set; }
        public int MaxAmmoCostPerFire { get; set; }
        public float MinChargeTime { get; set; }
        public float MaxChargeTime { get; set; }
        public float ChargeDownTime { get; set; }
        /// <summary>
        /// bAutoDischarge
        /// </summary>
        public bool AutoDischarge { get; set; }
        public float MaxChargeTimeUntilDischarge { get; set; }
        public float MinChargeDamageMultiplier { get; set; }
        public float MaxChargeDamageMultiplier { get; set; }
    }

    /// <summary>
    /// Only included if <see cref="HeatCapacity"/> is nonzero
    /// </summary>
    internal struct OverheatingInfo
    {
        /// <summary>
        /// OverheatingMaxValue
        /// </summary>
        public float HeatCapacity { get; set; }
        /// <summary>
        /// OverheatHeatingValue
        /// </summary>
        public float HeatingRate { get; set; }
        /// <summary>
        /// FullChargeOverheatHeatingValue
        /// </summary>
        public float FullChargeHeatingRate { get; set; }
        /// <summary>
        /// OverheatingCoolingValue
        /// </summary>
        public float CoolingRate { get; set; }
        /// <summary>
        /// FullyOverheatedCoolingValue
        /// </summary>
        public float OverheatedCoolingRate { get; set; }
        /// <summary>
        /// HeatingCooldownDelay
        /// </summary>
        public float CooldownDelay { get; set; }
        /// <summary>
        /// OverheatedCooldownDelay
        /// </summary>
        public float OverheatedCooldownDelay { get; set; }
    }

    internal struct DamageRange
    {
        public float Range { get; set; }
        public float Damage { get; set; }
        public float EnvDamage { get; set; }
        public float ImpactDamage { get; set; }
        public float KnockbackMagnitude { get; set; }
    }

    /// <summary>
    /// Only included for melee weapon schematics (i.e. RngMax is zero)
    /// </summary>
    internal class MeleeWeaponStats
    {
        /// <summary>
        /// RangeVSEnemies
        /// </summary>
        public float? RangeVsEnemies { get; set; }
        public float? SwingTime { get; set; }
        public float? Damage { get; set; }
        public float? EnvDamage { get; set; }
        public float? ImpactDamage { get; set; }
        public float? KnockbackMagnitude { get; set; }
        public float? KnockbackZAngle { get; set; }
        public int? Durability { get; set; }
        public float? DurabilityPerUse { get; set; }
        public float? BaseCritChance { get; set; }
        public float? BaseCritDamage { get; set; }
        public float? StunTime { get; set; }
    }

    /// <summary>
    /// Only included for trap schematics
    /// </summary>
    internal class TrapStats
    {
        public float? ArmTime { get; set; }
        public float? FireDelay { get; set; }
        public float? ReloadTime { get; set; }
        public float? Damage { get; set; }
        public float? ImpactDamage { get; set; }
        public float? KnockbackMagnitude { get; set; }
        public float? KnockbackZAngle { get; set; }
        public float? StunTime { get; set; }
        public int? Durability { get; set; }
        public float? BaseCritChance { get; set; }
        public float? BaseCritDamage { get; set; }
    }
}
