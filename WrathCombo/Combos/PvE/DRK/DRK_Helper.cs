﻿#region

using Dalamud.Game.ClientState.JobGauge.Types;
using Dalamud.Game.ClientState.Objects.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using WrathCombo.AutoRotation;
using WrathCombo.CustomComboNS;
using WrathCombo.CustomComboNS.Functions;
using WrathCombo.Data;
using static WrathCombo.CustomComboNS.Functions.CustomComboFunctions;
using BossAvoidance = WrathCombo.Combos.PvE.All.Enums.BossAvoidance;
using PartyRequirement = WrathCombo.Combos.PvE.All.Enums.PartyRequirement;
using Preset = WrathCombo.Combos.CustomComboPreset;

// ReSharper disable AccessToStaticMemberViaDerivedType
// ReSharper disable ConditionIsAlwaysTrueOrFalse
// ReSharper disable ReturnTypeCanBeNotNullable
// ReSharper disable UnusedType.Global
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable InconsistentNaming
// ReSharper disable CheckNamespace
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable MemberHidesStaticFromOuterClass

#endregion

namespace WrathCombo.Combos.PvE;

internal partial class DRK
{
    /// <summary>
    ///     Checking if there is space to weave an oGCD, with consideration for
    ///     whether triple weaves should be avoided or not.
    /// </summary>
    /// <seealso cref="CustomComboFunctions.CanWeave(double)" />
    /// <seealso cref="CanDelayedWeave(double,double)" />
    private static bool CanWeave =>
        (IsEnabled(Preset.DRK_ST_Adv) &&
         IsEnabled(Preset.DRK_PreventTripleWeaves) &&
         CanWeave() &&
         !ActionWatching.HasDoubleWeaved()) ||
        ((IsNotEnabled(Preset.DRK_ST_Adv) ||
          IsNotEnabled(Preset.DRK_PreventTripleWeaves)) &&
         (CanWeave() || CanDelayedWeave()));

    /// <summary>
    ///     DRK's job gauge.
    /// </summary>
    private static DRKGauge Gauge => GetJobGauge<DRKGauge>();

    /// <summary>
    ///     DRK's GCD, truncated to two decimal places.
    /// </summary>
    private static double GCD => GetCooldown(HardSlash).CooldownTotal;

    #region Burst Phase

    #region Variables

    /// When the current burst phase is set to truly start.
    private static long burstStartTime;

    /// When the current burst phase is set to end.
    private static long burstEndTime;

    /// The last time other jobs' buffs were checked for.
    private static DateTime lastBuffCheck = DateTime.MinValue;

    /// <summary>
    ///     Whether the player is being affected by other jobs' buffs.
    /// </summary>
    private static bool HasOtherJobsBuffs
    {
        get
        {
            // Only run every 1 seconds at most (should be every other burst check)
            if ((DateTime.Now - lastBuffCheck).TotalSeconds < 1)
                return field;

            field = TargetBuffRemainingTime(SCH.Debuffs.ChainStratagem) > 0 ||
                    BuffRemainingTime(AST.Buffs.Divination) > 0 ||
                    BuffRemainingTime(DRG.Buffs.BattleLitany) > 0 ||
                    TargetBuffRemainingTime(NIN.Debuffs.Mug) > 0 ||
                    TargetBuffRemainingTime(NIN.Debuffs.Dokumori) > 0 ||
                    BuffRemainingTime(MNK.Buffs.Brotherhood) > 0 ||
                    BuffRemainingTime(RPR.Buffs.ArcaneCircle) > 0 ||
                    BuffRemainingTime(BRD.Buffs.BattleVoice) > 0 ||
                    BuffRemainingTime(DNC.Buffs.TechnicalFinish) > 0 ||
                    BuffRemainingTime(SMN.Buffs.SearingLight) > 0 ||
                    BuffRemainingTime(RDM.Buffs.Embolden) > 0 ||
                    BuffRemainingTime(RDM.Buffs.EmboldenOthers) > 0 ||
                    BuffRemainingTime(PCT.Buffs.StarryMuse) > 0;

            lastBuffCheck = DateTime.Now;
            return field;

            // Just a shorter name for the methods
            double TargetBuffRemainingTime(ushort buff) =>
                GetStatusEffectRemainingTime(buff, CurrentTarget, anyOwner:true);
            double BuffRemainingTime(ushort buff) =>
                GetStatusEffectRemainingTime(buff, anyOwner:true);
        }
    }

    /// The last time burst phase was checked for.
    private static DateTime lastBurstCheck = DateTime.MinValue;

    #endregion

    /// <summary>
    /// Whether the DRK is in an even-minute Burst phase.
    /// </summary>
    internal static bool IsBursting
    {
        get
        {
            // Only run every .8 seconds at most
            if ((DateTime.Now - lastBurstCheck).TotalSeconds < 0.8)
                return field;

            var burstAbility = LivingShadow;
            var burstAbilityCDWindow = 90;
            if (!LevelChecked(LivingShadow))
            {
                burstAbilityCDWindow = 40;
                burstAbility = LevelChecked(Delirium) ? Delirium : BloodWeapon;
            }

            // Fallback resetting of burst
            if (GetCooldownRemainingTime(burstAbility) < 2 || !InCombat())
            {
                burstStartTime = 0;
                burstEndTime = 0;
                field = false;
            }

            if (GetCooldownRemainingTime(burstAbility) >= burstAbilityCDWindow)
            {
                // If the buff is active, start burst in 4s
                if (burstStartTime == 0)
                    burstStartTime = Environment.TickCount64 + 4000;

                // If the buff is active, set end time to 30s away
                if (burstEndTime == 0)
                    burstEndTime = Environment.TickCount64 + 30000;

                // Set to bursting
                if ((burstStartTime > 0 && Environment.TickCount64 > burstStartTime) ||
                    HasOtherJobsBuffs)
                {
                    burstStartTime = 0;
                    field = true;
                }
            }

            // Reset bursting
            if (burstEndTime > 0 && Environment.TickCount64 > burstEndTime)
            {
                burstEndTime = 0;
                field = false;
            }

            lastBurstCheck = DateTime.Now;
            return field;
        }
    }

    #endregion

    /// <summary>
    ///     Method for getting the player's target more reliably.
    /// </summary>
    /// <param name="flags">
    ///     The flags to describe the combo executing this method.
    /// </param>
    /// <param name="restrictToHostile">
    ///     Whether to restrict the target to hostile targets.<br />
    ///     Defaults to <c>true</c>.
    /// </param>
    /// <returns>
    ///     The player's current target, or the nearest target if AoE.
    /// </returns>
    private static IGameObject? Target(Combo flags, bool restrictToHostile = true)
    {
        switch (restrictToHostile)
        {
            case true when LocalPlayer.TargetObject is IBattleChara:
            case false when LocalPlayer.TargetObject is not null:
                return LocalPlayer.TargetObject;
        }

        if (flags.HasFlag(Combo.AoE))
            return AutoRotationController.DPSTargeting.BaseSelection
                .OrderByDescending(
                    x => GetTargetDistance(x))
                .FirstOrDefault();

        return null;
    }

    /// <summary>
    ///     Select the opener to use.
    /// </summary>
    /// <returns>A valid <see cref="WrathOpener">Opener</see>.</returns>
    internal static WrathOpener Opener()
    {
        if (Opener1.LevelChecked)
            return Opener1;

        return WrathOpener.Dummy;
    }

    #region Action Logic

    /// <remarks>
    ///     Actions in this Provider:
    ///     <list type="bullet">
    ///         <item>
    ///             <term>Variant Cure</term>
    ///         </item>
    ///         <item>
    ///             <term>Variant Ultimatum</term>
    ///         </item>
    ///         <item>
    ///             <term>Variant Spirit Dart</term>
    ///         </item>
    ///     </list>
    /// </remarks>
    private class VariantAction : IActionProvider
    {
        public bool TryGetAction(Combo flags, ref uint action, bool? _)
        {
            #region Heal

            if ((flags.HasFlag(Combo.Simple) ||
                 (flags.HasFlag(Combo.Adv) && IsEnabled(Preset.DRK_Var_Cure))) &&
                ActionReady(Variant.Cure) &&
                PlayerHealthPercentageHp() <= Config.DRK_VariantCure)
                return (action = Variant.Cure) != 0;

            #endregion

            // Bail if we can't weave anything
            if (!CanWeave) return false;

            #region Aggro + Stun

            if ((flags.HasFlag(Combo.Simple) ||
                 (flags.HasFlag(Combo.Adv) && IsEnabled(Preset.DRK_Var_Ulti))) &&
                ActionReady(Variant.Ultimatum))
                return (action = Variant.Ultimatum) != 0;

            #endregion

            #region Damage over Time

            if ((flags.HasFlag(Combo.Simple) ||
                 (flags.HasFlag(Combo.Adv) && IsEnabled(Preset.DRK_Var_Dart))) &&
                ActionReady(Variant.SpiritDart) &&
                GetStatusEffectRemainingTime(Content.Variant.Debuffs.SustainedDamage, CurrentTarget) <=3)
                return (action = Variant.SpiritDart) != 0;

            #endregion

            return false;
        }
    }

    /// <remarks>
    ///     Actions in this method:
    ///     <list type="bullet">
    ///         <item>
    ///             <term>Disesteem</term>
    ///         </item>
    ///         <item>
    ///             <term>Living Shadow</term>
    ///         </item>
    ///         <item>
    ///             <term>Interject</term>
    ///         </item>
    ///         <item>
    ///             <term>Low Blow</term>
    ///         </item>
    ///         <item>
    ///             <term>Delirium / Blood Weapon</term>
    ///         </item>
    ///         <item>
    ///             <term>Salted Earth</term>
    ///         </item>
    ///         <item>
    ///             <term>Salt and Darkness</term>
    ///         </item>
    ///         <item>
    ///             <term>Shadowbringer</term>
    ///         </item>
    ///         <item>
    ///             <term>Carve and Spit</term>
    ///             <description>(ST only)</description>
    ///         </item>
    ///         <item>
    ///             <term>Abyssal Drain</term>
    ///             <description>(AoE only)</description>
    ///         </item>
    ///     </list>
    /// </remarks>
    private class Cooldown : IActionProvider
    {
        public static bool ShouldDeliriumNext;

        public bool TryGetAction(Combo flags, ref uint action, bool? disesteemOnly)
        {
            #region Disesteem

            disesteemOnly ??= false;

            if ((flags.HasFlag(Combo.Simple) ||
                 IsSTEnabled(flags, Preset.DRK_ST_CD_Disesteem) ||
                 IsAoEEnabled(flags, Preset.DRK_AoE_CD_Disesteem)) &&
                ActionReady(Disesteem) &&
                TraitLevelChecked(Traits.EnhancedShadowIII) &&
                HasStatusEffect(Buffs.Scorn) &&
                ((Gauge.DarksideTimeRemaining > 0 &&
                  GetStatusEffectRemainingTime(Buffs.Scorn) < 24) ||
                 GetStatusEffectRemainingTime(Buffs.Scorn) < 14))
                return (action = OriginalHook(Disesteem)) != 0;

            #endregion

            if (!CanWeave || Gauge.DarksideTimeRemaining <= 1) return false;
            if (disesteemOnly == true) return false;

            if (HiddenFeaturesData.IsEnabledWith(
                    Preset.DRK_Hid_R6SHoldSquirrelBurst,
                    () => HiddenFeaturesData.Targeting.R6SSquirrel &&
                          CombatEngageDuration().TotalSeconds < 275))
                return false;

            #region Living Shadow

            #region Variables

            var shadowContentHPThreshold = flags.HasFlag(Combo.ST)
                ? Config.DRK_ST_LivingShadowThresholdDifficulty
                : Config.DRK_AoE_LivingShadowThresholdDifficulty;
            var shadowInHPContent =
                flags.HasFlag(Combo.Adv) && ContentCheck.IsInConfiguredContent(
                    shadowContentHPThreshold, ContentCheck.ListSet.Halved);
            var shadowHPThreshold = flags.HasFlag(Combo.ST)
                ? Config.DRK_ST_LivingShadowThreshold
                : Config.DRK_AoE_LivingShadowThreshold;
            var shadowHPMatchesThreshold =
                flags.HasFlag(Combo.Simple) || !shadowInHPContent ||
                (shadowInHPContent &&
                 GetTargetHPPercent(Target(flags)) > shadowHPThreshold);

            #endregion

            if ((flags.HasFlag(Combo.Simple) ||
                 IsSTEnabled(flags, Preset.DRK_ST_CD_Shadow) ||
                 IsAoEEnabled(flags, Preset.DRK_AoE_CD_Shadow)) &&
                IsOffCooldown(LivingShadow) &&
                LevelChecked(LivingShadow) &&
                shadowHPMatchesThreshold)
                return (action = LivingShadow) != 0;

            #endregion

            if (CombatEngageDuration().TotalSeconds <= 5) return false;

            #region Interrupting

            if ((flags.HasFlag(Combo.Simple) ||
                 IsSTEnabled(flags, Preset.DRK_ST_CD_Interrupt) ||
                 IsAoEEnabled(flags, Preset.DRK_AoE_Interrupt)) &&
                HiddenFeaturesData.IsEnabledWith(
                    Preset.DRK_Hid_R7SCircleCastOnly,
                    () => HiddenFeaturesData.Content.InR7S,
                    () => HiddenFeaturesData.Targeting.R7SCircleCastingAdd) &&
                Role.CanInterject())
                return (action = Role.Interject) != 0;

            if ((flags.HasFlag(Combo.Simple) ||
                 IsSTEnabled(flags, Preset.DRK_ST_CD_Stun) ||
                 IsAoEEnabled(flags, Preset.DRK_AoE_Stun)) &&
                !TargetIsBoss() &&
                !JustUsed(Role.Interject) &&
                Role.CanLowBlow() &&
                HiddenFeaturesData.IsEnabledWith(
                    Preset.DRK_Hid_R6SStunJabberOnly,
                    () => HiddenFeaturesData.Content.InR6S,
                    () => HiddenFeaturesData.Targeting.R6SJabber) &&
                !InBossEncounter())
                return (action = Role.LowBlow) != 0;

            #endregion

            #region Delirium (/Blood Weapon)

            #region Variables

            var deliriumContentHPThreshold = flags.HasFlag(Combo.ST)
                ? Config.DRK_ST_DeliriumThresholdDifficulty
                : Config.DRK_AoE_DeliriumThresholdDifficulty;
            var deliriumInHPContent =
                flags.HasFlag(Combo.Adv) && ContentCheck.IsInConfiguredContent(
                    deliriumContentHPThreshold, ContentCheck.ListSet.Halved);
            var deliriumHPThreshold = flags.HasFlag(Combo.ST)
                ? Config.DRK_ST_DeliriumThreshold
                : Config.DRK_AoE_DeliriumThreshold;
            var deliriumHPMatchesThreshold =
                flags.HasFlag(Combo.Simple) || !deliriumInHPContent ||
                (deliriumInHPContent &&
                 GetTargetHPPercent(Target(flags)) > deliriumHPThreshold);

            #endregion

            if ((flags.HasFlag(Combo.Simple) ||
                 IsSTEnabled(flags, Preset.DRK_ST_CD_Delirium) ||
                 IsAoEEnabled(flags, Preset.DRK_AoE_CD_Delirium)) &&
                deliriumHPMatchesThreshold &&
                LevelChecked(BloodWeapon) &&
                GetCooldownRemainingTime(BloodWeapon) < GCD*1.5)
                ShouldDeliriumNext = true;

            if (ShouldDeliriumNext &&
                IsOffCooldown(BloodWeapon))
            {
                ShouldDeliriumNext = false;
                return (action = OriginalHook(Delirium)) != 0;
            }

            #endregion

            #region Salted Earth

            #region Variables

            var saltStill =
                flags.HasFlag(Combo.Simple) || flags.HasFlag(Combo.ST) ||
                (flags.HasFlag(Combo.Adv) && flags.HasFlag(Combo.AoE) &&
                 IsNotEnabled(Preset.DRK_AoE_CD_SaltStill)) ||
                (flags.HasFlag(Combo.Adv) && flags.HasFlag(Combo.AoE) &&
                 IsEnabled(Preset.DRK_AoE_CD_SaltStill) && !IsMoving() &&
                 CombatEngageDuration().TotalSeconds >= 7);
            var saltHPThreshold =
                flags.HasFlag(Combo.AoE)
                    ? flags.HasFlag(Combo.Adv)
                        ? Config.DRK_AoE_SaltThreshold
                        : 30
                    : 0;

            #endregion

            if ((flags.HasFlag(Combo.Simple) ||
                 IsSTEnabled(flags, Preset.DRK_ST_CD_Salt) ||
                 IsAoEEnabled(flags, Preset.DRK_AoE_CD_Salt)) &&
                LevelChecked(SaltedEarth) &&
                IsOffCooldown(SaltedEarth) &&
                !HasStatusEffect(Buffs.SaltedEarth) &&
                saltStill &&
                GetTargetHPPercent(Target(flags)) >= saltHPThreshold)
                return (action = SaltedEarth) != 0;

            #endregion

            #region Salt and Darkness

            if ((flags.HasFlag(Combo.Simple) ||
                 flags.HasFlag(Combo.AoE) ||
                 IsEnabled(Preset.DRK_ST_CD_Darkness)) &&
                LevelChecked(SaltAndDarkness) &&
                IsOffCooldown(SaltAndDarkness) &&
                HasStatusEffect(Buffs.SaltedEarth) &&
                GetStatusEffectRemainingTime(Buffs.SaltedEarth) < 7)
                return (action = OriginalHook(SaltAndDarkness)) != 0;

            #endregion

            #region Shadowbringer

            #region Variables

            var bringerInBurst =
                flags.HasFlag(Combo.Simple) || flags.HasFlag(Combo.AoE) ||
                (flags.HasFlag(Combo.Adv) && flags.HasFlag(Combo.ST) &&
                 !IsEnabled(Preset.DRK_ST_CD_BringerBurst)) ||
                (flags.HasFlag(Combo.Adv) && flags.HasFlag(Combo.ST) &&
                 IsEnabled(Preset.DRK_ST_CD_BringerBurst) &&
                 GetCooldownRemainingTime(LivingShadow) >= 90 &&
                 !HasStatusEffect(Buffs.Scorn));

            #endregion

            if ((flags.HasFlag(Combo.Simple) ||
                 IsSTEnabled(flags, Preset.DRK_ST_CD_Bringer) ||
                 IsAoEEnabled(flags, Preset.DRK_AoE_CD_Bringer)) &&
                ActionReady(Shadowbringer) &&
                bringerInBurst)
                return (action = Shadowbringer) != 0;

            #endregion

            #region Carve and Spit (ST only)

            if (flags.HasFlag(Combo.ST) &&
                (flags.HasFlag(Combo.Simple) ||
                 IsEnabled(Preset.DRK_ST_CD_Spit)) &&
                ActionReady(CarveAndSpit) &&
                (int)LocalPlayer.CurrentMp <= 9400 &&
                (!LevelChecked(LivingShadow) ||
                 GetCooldownRemainingTime(LivingShadow) > 20))
                return (action = CarveAndSpit) != 0;

            #endregion

            #region Abyssal Drain (AoE only)

            #region Variables

            var drainHPThreshold = flags.HasFlag(Combo.Adv)
                ? Config.DRK_AoE_DrainThreshold
                : 60;

            #endregion

            if (flags.HasFlag(Combo.AoE) &&
                (flags.HasFlag(Combo.Simple) ||
                 IsEnabled(Preset.DRK_AoE_CD_Drain)) &&
                ActionReady(AbyssalDrain) &&
                PlayerHealthPercentageHp() <= drainHPThreshold)
                return (action = AbyssalDrain) != 0;

            #endregion

            return false;
        }
    }

    #region JustUsedMit

    private static bool InSavagePlus => ContentCheck.IsInSavagePlusContent;

    /// <summary>
    ///     Whether mitigation was very recently used, depending on the duration and
    ///     strength of the mitigation.
    /// </summary>
    private static bool JustUsedMitigation =>
        JustUsed(BlackestNight, (InSavagePlus ? 3f : 4f)) ||
        JustUsed(Oblation, (InSavagePlus ? 6f : 4f)) ||
        JustUsed(Role.Reprisal, (InSavagePlus ? 1f : 4f)) ||
        JustUsed(DarkMissionary, (InSavagePlus ? 0f : 5f)) ||
        JustUsed(Role.Rampart, 6f) ||
        JustUsed(Role.ArmsLength, (InSavagePlus ? 0f : 4f)) ||
        JustUsed(ShadowedVigil, (InSavagePlus ? 11f : 6f)) ||
        JustUsed(LivingDead, (InSavagePlus ? 13f : 7f));

    #endregion

    /// <remarks>
    ///     Actions in this Provider:
    ///     <list type="bullet">
    ///         <item>
    ///             <term>Living Dead</term>
    ///         </item>
    ///         <item>
    ///             <term>TBN</term>
    ///         </item>
    ///         <item>
    ///             <term>Oblation</term>
    ///         </item>
    ///         <item>
    ///             <term>Reprisal</term>
    ///         </item>
    ///         <item>
    ///             <term>Dark Missionary</term>
    ///             <description>(ST only)</description>
    ///         </item>
    ///         <item>
    ///             <term>Rampart</term>
    ///             <description>(AoE only)</description>
    ///         </item>
    ///         <item>
    ///             <term>Arms Length</term>
    ///             <description>(AoE only)</description>
    ///         </item>
    ///         <item>
    ///             <term>Shadowed Vigil</term>
    ///         </item>
    ///     </list>
    /// </remarks>
    private class Mitigation : IActionProvider
    {
        public bool TryGetAction(Combo flags, ref uint action, bool? _)
        {
            // Bail if we're trying to Invuln or actively Invulnerable
            if (HasStatusEffect(Buffs.LivingDead) ||
                HasStatusEffect(Buffs.WalkingDead) ||
                HasStatusEffect(Buffs.UndeadRebirth))
                return false;

            // Bail if Simple mode and mitigation is disabled
            if (flags.HasFlag(Combo.Simple) &&
                ((flags.HasFlag(Combo.ST) &&
                  (int)Config.DRK_ST_SimpleMitigation ==
                  (int)Config.SimpleMitigation.Off) ||
                 (flags.HasFlag(Combo.AoE) &&
                  (int)Config.DRK_AoE_SimpleMitigation ==
                  (int)Config.SimpleMitigation.Off)))
                return false;

            #region Living Dead

            #region Variables

            var bossRestrictionLivingDead = flags.HasFlag(Combo.Adv)
                ? (int)Config.DRK_ST_LivingDeadBossRestriction
                : (int)BossAvoidance.Off;
            var livingDeadSelfThreshold = flags.HasFlag(Combo.Adv) ?
                flags.HasFlag(Combo.ST)
                    ? Config.DRK_ST_LivingDeadSelfThreshold
                    : Config.DRK_AoE_LivingDeadSelfThreshold :
                flags.HasFlag(Combo.ST) ? 15 : 20;
            var livingDeadTargetThreshold = flags.HasFlag(Combo.Adv) ?
                flags.HasFlag(Combo.ST)
                    ? Config.DRK_ST_LivingDeadTargetThreshold
                    : Config.DRK_AoE_LivingDeadTargetThreshold :
                flags.HasFlag(Combo.ST) ? 1 : 15;

            #endregion

            if ((flags.HasFlag(Combo.Simple) ||
                 IsSTEnabled(flags, Preset.DRK_ST_Mit_LivingDead) ||
                 IsAoEEnabled(flags, Preset.DRK_AoE_Mit_LivingDead)) &&
                ActionReady(LivingDead) &&
                PlayerHealthPercentageHp() <= livingDeadSelfThreshold &&
                GetTargetHPPercent(Target(flags)) >= livingDeadTargetThreshold &&
                // Checking if the target matches the boss avoidance option
                ((bossRestrictionLivingDead is (int)BossAvoidance.On &&
                  InBossEncounter()) ||
                 bossRestrictionLivingDead is (int)BossAvoidance.Off))
                return (action = LivingDead) != 0;

            #endregion

            // Bail if we can't weave any other mitigations
            if (!CanWeave) return false;
            // Bail if we just used mitigation
            if (JustUsedMitigation) return false;

            #region TBN

            if ((flags.HasFlag(Combo.Simple) ||
                 IsSTEnabled(flags, Preset.DRK_ST_Mit_TBN) ||
                 IsAoEEnabled(flags, Preset.DRK_AoE_Mit_TBN)) &&
                ActionReady(BlackestNight) &&
                LocalPlayer.CurrentMp >= 3000 &&
                ShouldTBNSelf(flags.HasFlag(Combo.AoE)))
                return (action = BlackestNight) != 0;

            #endregion

            #region Oblation

            #region Variables

            var oblationCharges = flags.HasFlag(Combo.Adv)
                ? flags.HasFlag(Combo.ST)
                    ? Config.DRK_ST_OblationCharges
                    : Config.DRK_AoE_OblationCharges
                : 0;
            var oblationThreshold = flags.HasFlag(Combo.Adv)
                ? flags.HasFlag(Combo.ST)
                    ? Config.DRK_ST_Mit_OblationThreshold
                    : Config.DRK_AoE_Mit_OblationThreshold
                : 90;

            #endregion

            if ((flags.HasFlag(Combo.Simple) ||
                 IsSTEnabled(flags, Preset.DRK_ST_Mit_Oblation) ||
                 IsAoEEnabled(flags, Preset.DRK_AoE_Mit_Oblation)) &&
                ActionReady(Oblation) &&
                !HasStatusEffect(Buffs.Oblation, anyOwner: true) &&
                GetRemainingCharges(Oblation) > oblationCharges &&
                PlayerHealthPercentageHp() <= oblationThreshold)
                return (action = Oblation) != 0;

            #endregion

            #region Reprisal

            #region Variables

            var reprisalThreshold =
                flags.HasFlag(Combo.Adv) && flags.HasFlag(Combo.AoE)
                    ? Config.DRK_AoE_Mit_ReprisalThreshold
                    : 100;
            var reprisalTargetCount =
                flags.HasFlag(Combo.Adv) && flags.HasFlag(Combo.AoE)
                    ? Config.DRK_AoE_ReprisalEnemyCount
                    : 1;
            var reprisalUseForRaidwides =
                flags.HasFlag(Combo.AoE) || RaidWideCasting();

            #endregion

            if ((flags.HasFlag(Combo.Simple) ||
                 IsSTEnabled(flags, Preset.DRK_ST_Mit_Reprisal) ||
                 IsAoEEnabled(flags, Preset.DRK_AoE_Mit_Reprisal)) &&
                reprisalUseForRaidwides &&
                Role.CanReprisal(reprisalThreshold, reprisalTargetCount,
                    !flags.HasFlag(Combo.AoE)) &&
                HiddenFeaturesData.IsEnabledWith(
                    Preset.DRK_Hid_R6SNoAutoGroupMits,
                    () => !HiddenFeaturesData.Content.InR6S))
                return (action = Role.Reprisal) != 0;

            #endregion

            #region Dark Missionary (ST only)

            #region Variables

            var missionaryThreshold =
                flags.HasFlag(Combo.Adv) && flags.HasFlag(Combo.ST)
                    ? Config.DRK_ST_Mit_MissionaryThreshold
                    : 100;
            var missionaryAvoidanceSatisfied =
                flags.HasFlag(Combo.AoE) ||
                flags.HasFlag(Combo.Simple) ||
                IsNotEnabled(Preset.DRK_ST_Mit_MissionaryAvoid) ||
                !HasStatusEffect(Role.Debuffs.Reprisal, CurrentTarget, true);

            #endregion

            if (flags.HasFlag(Combo.ST) &&
                (flags.HasFlag(Combo.Simple) ||
                 IsEnabled(Preset.DRK_ST_Mit_Missionary)) &&
                ActionReady(DarkMissionary) &&
                RaidWideCasting() &&
                missionaryAvoidanceSatisfied &&
                PlayerHealthPercentageHp() <= missionaryThreshold &&
                HiddenFeaturesData.IsEnabledWith(
                    Preset.DRK_Hid_R6SNoAutoGroupMits,
                    () => !HiddenFeaturesData.Content.InR6S))
                return (action = DarkMissionary) != 0;

            #endregion

            #region Rampart (AoE only)

            #region Variables

            var rampartThreshold =
                flags.HasFlag(Combo.Adv) && flags.HasFlag(Combo.AoE)
                    ? Config.DRK_AoE_Mit_RampartThreshold
                    : 100;

            #endregion

            if (flags.HasFlag(Combo.AoE) &&
                (flags.HasFlag(Combo.Simple) ||
                 IsEnabled(Preset.DRK_AoE_Mit_Rampart)) &&
                Role.CanRampart(rampartThreshold))
                return (action = Role.Rampart) != 0;

            #endregion

            #region Arms Length (AoE only)

            #region Variables

            var armsLengthEnemyCount = flags.HasFlag(Combo.Adv)
                ? Config.DRK_AoE_ArmsLengthEnemyCount
                : 3;

            #endregion

            if (flags.HasFlag(Combo.AoE) &&
                (flags.HasFlag(Combo.Simple) ||
                 IsEnabled(Preset.DRK_AoE_Mit_ArmsLength)) &&
                Role.CanArmsLength(armsLengthEnemyCount))
                return (action = Role.ArmsLength) != 0;

            #endregion

            #region Shadowed Vigil

            #region Variables

            var vigilHealthThreshold = flags.HasFlag(Combo.Adv) ?
                flags.HasFlag(Combo.ST)
                    ? Config.DRK_ST_ShadowedVigilThreshold
                    : Config.DRK_AoE_ShadowedVigilThreshold :
                flags.HasFlag(Combo.ST) ? 40 : 50;

            #endregion

            if ((flags.HasFlag(Combo.Simple) ||
                 IsSTEnabled(flags, Preset.DRK_ST_Mit_Vigil) ||
                 IsAoEEnabled(flags, Preset.DRK_AoE_Mit_Vigil)) &&
                ActionReady(ShadowedVigil) &&
                PlayerHealthPercentageHp() <= vigilHealthThreshold)
                return (action = OriginalHook(ShadowWall)) != 0;

            #endregion

            return false;
        }
    }

    /// <remarks>
    ///     Actions in this Provider:
    ///     <list type="bullet">
    ///         <item>
    ///             <term>Bloodspiller</term>
    ///         </item>
    ///         <item>
    ///             <term>Quietus</term>
    ///         </item>
    ///         <item>
    ///             <term>Scarlet Delirium</term>
    ///         </item>
    ///         <item>
    ///             <term>Comeuppance</term>
    ///         </item>
    ///         <item>
    ///             <term>Torcleaver</term>
    ///         </item>
    ///         <item>
    ///             <term>Edge of Darkness</term>
    ///         </item>
    ///         <item>
    ///             <term>Edge of Shadow</term>
    ///         </item>
    ///         <item>
    ///             <term>Flood of Darkness</term>
    ///         </item>
    ///         <item>
    ///             <term>Flood of Shadow</term>
    ///         </item>
    ///     </list>
    /// </remarks>
    private class Spender : IActionProvider
    {
        public bool TryGetAction(Combo flags, ref uint action, bool? specialManaOnly)
        {
            if (TryGetManaAction(flags, ref action, specialManaOnly)) return true;
            if (specialManaOnly == true) return false;
            if (TryGetBloodAction(flags, ref action)) return true;

            return false;
        }

        private bool TryGetBloodAction(Combo flags, ref uint action)
        {
            if (ComboTimer > 0 && ComboTimer < GCD * 2) return false;

            #region Variables and readiness bails

            var bloodGCDReady =
                LevelChecked(Bloodspiller) &&
                GetCooldownRemainingTime(Bloodspiller) < GCD/2;

            if (!bloodGCDReady) return false;

            #endregion

            #region Delirium Chain

            if ((flags.HasFlag(Combo.Simple) ||
                 IsSTEnabled(flags, Preset.DRK_ST_Sp_ScarletChain) ||
                 IsAoEEnabled(flags, Preset.DRK_AoE_Sp_ImpalementChain)) &&
                HasStatusEffect(Buffs.EnhancedDelirium) &&
                GetStatusEffectStacks(Buffs.EnhancedDelirium) > 0)
                if (flags.HasFlag(Combo.ST))
                    return (action = OriginalHook(Bloodspiller)) != 0;
                else if (flags.HasFlag(Combo.AoE))
                    return (action = OriginalHook(Quietus)) != 0;

            #endregion

            #region Blood Spending during Delirium (Lower Levels)

            if ((flags.HasFlag(Combo.Simple) ||
                 IsSTEnabled(flags, Preset.DRK_ST_Sp_Bloodspiller) ||
                 IsAoEEnabled(flags, Preset.DRK_AoE_Sp_Quietus)) &&
                GetStatusEffectStacks(Buffs.Delirium) > 0)
                if (flags.HasFlag(Combo.ST))
                    return (action = OriginalHook(Bloodspiller)) != 0;
                else if (flags.HasFlag(Combo.AoE))
                    return (action = OriginalHook(Quietus)) != 0;

            #endregion

            #region Blood Spending prior to Delirium (ST only)

            if (flags.HasFlag(Combo.ST) &&
                (flags.HasFlag(Combo.Simple) ||
                 IsEnabled(Preset.DRK_ST_CD_Delirium)) &&
                LevelChecked(Delirium) &&
                Gauge.Blood >= 70 &&
                Cooldown.ShouldDeliriumNext)
                return (action = Bloodspiller) != 0;

            #endregion

            if (HasStatusEffect(Buffs.Scorn)) return false;

            #region Blood Spending after Delirium Chain

            if ((flags.HasFlag(Combo.Simple) ||
                 IsSTEnabled(flags, Preset.DRK_ST_Sp_Bloodspiller) ||
                 IsAoEEnabled(flags, Preset.DRK_AoE_Sp_Quietus)) &&
                Gauge.Blood >= 50 &&
                (GetCooldownRemainingTime(Delirium) > 37 || IsBursting))
                if (flags.HasFlag(Combo.ST))
                    return (action = Bloodspiller) != 0;
                else if (flags.HasFlag(Combo.AoE) && LevelChecked(Quietus))
                    return (action = Quietus) != 0;

            #endregion

            #region Blood Overcap

            #region Variables

            var overcapThreshold = flags.HasFlag(Combo.Adv)
                ? flags.HasFlag(Combo.ST)
                    ? Config.DRK_ST_BloodOvercapThreshold
                    : Config.DRK_AoE_BloodOvercapThreshold
                : 90;

            var beforeSouleater =
                flags.HasFlag(Combo.AoE) || ComboAction == SyphonStrike;

            #endregion

            if ((flags.HasFlag(Combo.Simple) ||
                 IsSTEnabled(flags, Preset.DRK_ST_Sp_BloodOvercap) ||
                 IsAoEEnabled(flags, Preset.DRK_AoE_Sp_BloodOvercap)) &&
                Gauge.Blood >= overcapThreshold &&
                beforeSouleater)
                if (flags.HasFlag(Combo.ST))
                    return (action = Bloodspiller) != 0;
                else if (flags.HasFlag(Combo.AoE) && LevelChecked(Quietus))
                    return (action = Quietus) != 0;

            #endregion

            return false;
        }

        private bool TryGetManaAction(Combo flags, ref uint action, bool? specialOnly)
        {
            // Bail if we can't weave anything else
            if (!CanWeave) return false;

            #region Variables and some Mana bails

            // Bail if it is too early into the fight
            if (CombatEngageDuration().TotalSeconds <= 5) return false;
            // Bail if mana spending is not available yet
            if (!LevelChecked(FloodOfDarkness)) return false;

            var mana = (int)LocalPlayer.CurrentMp;
            var manaPooling =
                ContentCheck.IsInConfiguredContent(
                    Config.DRK_ST_ManaSpenderPoolingDifficulty,
                    Config.DRK_ST_ManaSpenderPoolingDifficultyListSet);
            var manaPool = flags.HasFlag(Combo.Adv)
                ? flags.HasFlag(Combo.ST)
                    ? manaPooling ? (int)Config.DRK_ST_ManaSpenderPooling : 0
                    : (int)Config.DRK_AoE_ManaSpenderPooling
                : 0;
            var hasEnoughMana = mana >= (manaPool + 3000) || Gauge.HasDarkArts;
            var secondsBeforeBurst =
                flags.HasFlag(Combo.Adv) && flags.HasFlag(Combo.ST)
                    ? Config.DRK_ST_BurstSoonThreshold
                    : 20;
            var evenBurstSoon =
                IsOnCooldown(LivingShadow) &&
                GetCooldownRemainingTime(LivingShadow) < secondsBeforeBurst;
            var darksideDropping = Gauge.DarksideTimeRemaining / 1000 < 10;

            // Bail if we don't have enough mana
            if (!hasEnoughMana) return false;

            #endregion

            #region Darkside Maintenance

            if ((flags.HasFlag(Combo.Simple) ||
                 IsSTEnabled(flags, Preset.DRK_ST_Sp_EdgeDarkside) ||
                 IsAoEEnabled(flags, Preset.DRK_AoE_Sp_Flood)) &&
                darksideDropping)
                if (flags.HasFlag(Combo.ST) && LevelChecked(EdgeOfDarkness))
                    return (action = OriginalHook(EdgeOfDarkness)) != 0;
                else
                    return (action = OriginalHook(FloodOfDarkness)) != 0;

            #endregion

            // Bail if it is right before burst
            if (GetCooldownRemainingTime(LivingShadow) <
                Math.Min(6, secondsBeforeBurst) &&
                LevelChecked(LivingShadow) &&
                CombatEngageDuration().TotalSeconds > 20)
                return false;

            #region Mana Overcap

            if ((flags.HasFlag(Combo.Simple) ||
                 IsSTEnabled(flags, Preset.DRK_ST_Sp_ManaOvercap) ||
                 IsAoEEnabled(flags, Preset.DRK_AoE_Sp_ManaOvercap)) &&
                mana >= 9400 &&
                !evenBurstSoon)
                if (flags.HasFlag(Combo.ST) && LevelChecked(EdgeOfDarkness))
                    return (action = OriginalHook(EdgeOfDarkness)) != 0;
                else
                    return (action = OriginalHook(FloodOfDarkness)) != 0;

            #endregion

            if (specialOnly == true) return false;

            #region Burst Phase Spending

            if ((flags.HasFlag(Combo.Simple) ||
                 IsSTEnabled(flags, Preset.DRK_ST_Sp_Edge) ||
                 IsAoEEnabled(flags, Preset.DRK_AoE_Sp_Flood)) &&
                IsBursting)
                if (flags.HasFlag(Combo.ST) && LevelChecked(EdgeOfDarkness))
                    return (action = OriginalHook(EdgeOfDarkness)) != 0;
                else
                    return (action = OriginalHook(FloodOfDarkness)) != 0;

            #endregion

            // Bail if it is too early into the fight
            if (CombatEngageDuration().TotalSeconds <= 10) return false;

            #region Mana Dark Arts Drop Prevention

            if ((flags.HasFlag(Combo.Simple) ||
                 IsSTEnabled(flags, Preset.DRK_ST_Sp_DarkArts) ||
                 IsAoEEnabled(flags, Preset.DRK_AoE_Sp_Flood)) &&
                Gauge.HasDarkArts && HasOwnTBN)
                if (flags.HasFlag(Combo.ST) && LevelChecked(EdgeOfDarkness))
                    return (action = OriginalHook(EdgeOfDarkness)) != 0;
                else
                    return (action = OriginalHook(FloodOfDarkness)) != 0;

            #endregion

            return false;
        }
    }

    /// <remarks>
    ///     Will almost always return <c>true</c>.<br />
    ///     Actions in this Provider:
    ///     <list type="bullet">
    ///         <item>
    ///             <term>Hard Slash</term>
    ///         </item>
    ///         <item>
    ///             <term>Syphon Strike</term>
    ///         </item>
    ///         <item>
    ///             <term>Souleater</term>
    ///         </item>
    ///         <item>
    ///             <term>Unleash</term>
    ///         </item>
    ///         <item>
    ///             <term>Stalwart Soul</term>
    ///         </item>
    ///     </list>
    /// </remarks>
    private class Core : IActionProvider
    {
        public bool TryGetAction(Combo flags, ref uint action, bool? _)
        {
            var comboRunning = ComboTimer > 0;
            var lastComboAction = ComboAction;

            #region Single-Target 1-2-3 Combo

            if (flags.HasFlag(Combo.ST))
                if (!comboRunning)
                    return (action = HardSlash) != 0;
                else if (lastComboAction == HardSlash &&
                         LevelChecked(SyphonStrike))
                    return (action = SyphonStrike) != 0;
                else if (lastComboAction == SyphonStrike &&
                         LevelChecked(Souleater))
                    return (action = Souleater) != 0;

            #endregion

            #region AoE 1-2 Combo

            if (flags.HasFlag(Combo.AoE))
                if (!comboRunning)
                    return (action = Unleash) != 0;
                else if (lastComboAction == Unleash &&
                         LevelChecked(StalwartSoul))
                    return (action = StalwartSoul) != 0;

            #endregion

            return false;
        }
    }

    #region TBN

    /// <summary>
    ///     Whether the player has a shield from TBN from themselves.
    /// </summary>
    /// <seealso cref="Buffs.BlackestNightShield" />
    private static bool HasOwnTBN
    {
        get
        {
            var has = false;
            if (LocalPlayer is not null)
                has = HasStatusEffect(Buffs.BlackestNightShield);

            return has;
        }
    }

    /// <summary>
    ///     Whether the player has a shield from TBN from anyone.
    /// </summary>
    /// <seealso cref="Buffs.BlackestNightShield" />
    private static bool HasAnyTBN
    {
        get
        {
            var has = false;
            if (LocalPlayer is not null)
                has = HasStatusEffect(Buffs.BlackestNightShield, anyOwner: true);

            return has;
        }
    }

    /// <summary>
    ///     Decides if the player should use TBN on themselves,
    ///     based on general rules and the player's configuration.
    /// </summary>
    /// <param name="aoe">Whether AoE or ST options should be checked.</param>
    /// <returns>Whether TBN should be used on self.</returns>
    /// <seealso cref="BlackestNight" />
    /// <seealso cref="Buffs.BlackestNightShield" />
    /// <seealso cref="CustomComboPreset.DRK_ST_Mit_TBN" />
    /// <seealso cref="Config.DRK_ST_TBNThreshold" />
    /// <seealso cref="Config.DRK_ST_TBNBossRestriction" />
    /// <seealso cref="CustomComboPreset.DRK_AoE_Mit_TBN" />
    private static bool ShouldTBNSelf(bool aoe = false)
    {
        // Bail if we're dead or unloaded
        if (LocalPlayer is null)
            return false;

        // Bail if TBN is disabled
        if ((!aoe
             && (!IsEnabled(Preset.DRK_ST_Mitigation)
                 || !IsEnabled(Preset.DRK_ST_Mit_TBN)))
            || (aoe
                && (!IsEnabled(Preset.DRK_AoE_Mitigation)
                    || !IsEnabled(Preset.DRK_AoE_Mit_TBN))))
            return false;

        // Bail if we already have TBN
        if (HasOwnTBN)
            return false;

        // Bail if we have no target
        if (!HasBattleTarget())
            return false;

        var hpRemaining = PlayerHealthPercentageHp();
        var hpThreshold = !aoe ? (float)Config.DRK_ST_TBNThreshold : 90f;

        // Bail if we're above the threshold
        if (hpRemaining > hpThreshold)
            return false;

        var targetIsBoss = TargetIsBoss();
        var bossRestriction = !aoe
            ? (int)Config.DRK_ST_TBNBossRestriction
            : (int)BossAvoidance.Off; // Don't avoid bosses in AoE

        // Bail if we're trying to avoid bosses and the target is one
        if (bossRestriction is (int)BossAvoidance.On
            && targetIsBoss)
            return false;

        // Bail if we have a TBN and burst is >30s away ()
        if (GetCooldownRemainingTime(LivingShadow) > 30
            && HasAnyTBN)
            return false;

        return true;
    }

    #endregion

    #region One-Button Mitigation

    /// <summary>
    ///     The list of Mitigations to use in the One-Button Mitigation combo.<br />
    ///     The order of the list needs to match the order in
    ///     <see cref="CustomComboPreset" />.
    /// </summary>
    /// <value>
    ///     <c>Action</c> is the action to use.<br />
    ///     <c>Preset</c> is the preset to check if the action is enabled.<br />
    ///     <c>Logic</c> is the logic for whether to use the action.
    /// </value>
    /// <remarks>
    ///     Each logic check is already combined with checking if the preset
    ///     <see cref="IsEnabled">is enabled</see>
    ///     and if the action is <see cref="ActionReady(uint)">ready</see> and
    ///     <see cref="LevelChecked(uint)">level-checked</see>.<br />
    ///     Do not add any of these checks to <c>Logic</c>.
    /// </remarks>
    private static (uint Action, Preset Preset, System.Func<bool> Logic)[]
        PrioritizedMitigation =>
    [
        (BlackestNight, Preset.DRK_Mit_TheBlackestNight,
            () => !HasAnyTBN && LocalPlayer.CurrentMp > 3000 &&
                  PlayerHealthPercentageHp() <= Config.DRK_Mit_TBN_Health),
        (Oblation, Preset.DRK_Mit_Oblation,
            () => (!((TargetIsFriendly() && HasStatusEffect(Buffs.Oblation, CurrentTarget, true)) ||
                     (!TargetIsFriendly() && HasStatusEffect(Buffs.Oblation, anyOwner: true)))) &&
                  GetRemainingCharges(Oblation) > Config.DRK_Mit_Oblation_Charges),
        (Role.Reprisal, Preset.DRK_Mit_Reprisal,
            () => Role.CanReprisal(checkTargetForDebuff:false)),
        (DarkMissionary, Preset.DRK_Mit_DarkMissionary,
            () => Config.DRK_Mit_DarkMissionary_PartyRequirement ==
                  (int)PartyRequirement.No || IsInParty()),
        (Role.Rampart, Preset.DRK_Mit_Rampart,
            () => Role.CanRampart(Config.DRK_Mit_Rampart_Health)),
        (DarkMind, Preset.DRK_Mit_DarkMind, () => true),
        (Role.ArmsLength, Preset.DRK_Mit_ArmsLength,
            () => Role.CanArmsLength(Config.DRK_Mit_ArmsLength_EnemyCount, Config.DRK_Mit_ArmsLength_Boss)),
        (OriginalHook(ShadowWall), Preset.DRK_Mit_ShadowWall,
            () => PlayerHealthPercentageHp() <= Config.DRK_Mit_ShadowWall_Health),
    ];

    /// <summary>
    ///     Given the index of a mitigation in <see cref="PrioritizedMitigation" />,
    ///     checks if the mitigation is ready and meets the provided requirements.
    /// </summary>
    /// <param name="index">
    ///     The index of the mitigation in <see cref="PrioritizedMitigation" />,
    ///     which is the order of the mitigation in <see cref="CustomComboPreset" />.
    /// </param>
    /// <param name="action">
    ///     The variable to set to the action to, if the mitigation is set to be
    ///     used.
    /// </param>
    /// <returns>
    ///     Whether the mitigation is ready, enabled, and passes the provided logic
    ///     check.
    /// </returns>
    private static bool CheckMitigationConfigMeetsRequirements
        (int index, out uint action)
    {
        action = PrioritizedMitigation[index].Action;
        return ActionReady(action) &&
               PrioritizedMitigation[index].Logic() &&
               IsEnabled(PrioritizedMitigation[index].Preset);
    }

    #endregion

    #endregion

    #region Openers

    private static void handleEdgeCasts
        (uint currentAction, ref uint action, uint[] castLocations)
    {
        if (castLocations.Contains(currentAction) &&
            (Gauge.HasDarkArts || LocalPlayer.CurrentMp > 3000) &&
            CanWeave() && !ActionWatching.HasDoubleWeaved())
            action = OriginalHook(EdgeOfDarkness);
    }

    internal static DRKOpenerMaxLevel1 Opener1 = new();

    internal class DRKOpenerMaxLevel1 : WrathOpener
    {
        public override int MinOpenerLevel => 100;

        public override int MaxOpenerLevel => 109;

        public override List<uint> OpenerActions { get; set; } =
        [
            Unmend,
            HardSlash,
            EdgeOfShadow, // Not handled like a procc, since it sets up Darkside
            LivingShadow,
            SyphonStrike, // 5
            LivingShadow,
            Souleater,
            Delirium,
            HardSlash,
            Disesteem, // 10
            SaltedEarth,
            //EdgeOfShadow, // Handled like a procc
            ScarletDelirium,
            Shadowbringer,
            //EdgeOfShadow, // Handled like a procc
            Comeuppance,
            CarveAndSpit, // 15
            //EdgeOfShadow, // Handled like a procc
            Torcleaver,
            Shadowbringer,
            //EdgeOfShadow, // Handled like a procc
            Bloodspiller,
            SaltAndDarkness,
        ];

        public override List<(int[] Steps, uint NewAction, Func<bool> Condition)> SubstitutionSteps
        {
            get;
            set;
        } =
        [
            // Pull with Shadowstride as selected
            ([1], Shadowstride, () =>
                Config.DRK_ST_OpenerAction == (int)Config.PullAction.Shadowstride),
            // Pull with HardSlash as selected (requires skipping the now-duplicate HardSlash)
            ([1], HardSlash, () =>
                Config.DRK_ST_OpenerAction == (int)Config.PullAction.HardSlash),
        ];

        public override List<(int[] Steps, Func<bool> Condition)> SkipSteps
        {
            get;
            set;
        } =
        [
            // Skip the duplicate HardSlash, if pulling with HardSlash
            ([2], () =>
                Config.DRK_ST_OpenerAction == (int)Config.PullAction.HardSlash),
            // Skip the early LivingShadow, if non-standard
            ([4], () =>
                Config.DRK_ST_OpenerAction != (int)Config.PullAction.Unmend),
            // Skip the late LivingShadow and aligning HardSlash, if Standard
            ([6, 9], () => HasOtherJobsBuffs ||
                Config.DRK_ST_OpenerAction == (int)Config.PullAction.Unmend),
        ];

        internal override UserData? ContentCheckConfig =>
            Config.DRK_ST_OpenerDifficulty;

        public override bool HasCooldowns()
        {
            if (LocalPlayer.CurrentMp < 7000)
                return false;

            if (!IsOffCooldown(LivingShadow))
                return false;

            if (!IsOffCooldown(Delirium))
                return false;

            if (!IsOffCooldown(CarveAndSpit))
                return false;

            if (!IsOffCooldown(SaltedEarth))
                return false;

            if (GetRemainingCharges(Shadowbringer) < 2)
                return false;

            return true;
        }
    }

    #endregion

    #region IDs

    public const byte JobID = 32;

    #region Actions

    public const uint

    #region Single-Target 1-2-3 Combo

        HardSlash = 3617,
        SyphonStrike = 3623,
        Souleater = 3632,

    #endregion

    #region AoE 1-2-3 Combo

        Unleash = 3621,
        StalwartSoul = 16468,

    #endregion

    #region Single-Target oGCDs

        CarveAndSpit = 3643, // With AbyssalDrain
        EdgeOfDarkness = 16467, // For MP
        EdgeOfShadow = 16470, // For MP // Upgrade of EdgeOfDarkness
        Bloodspiller = 7392, // For Blood
        ScarletDelirium = 36928, // Under Enhanced Delirium
        Comeuppance = 36929, // Under Enhanced Delirium
        Torcleaver = 36930, // Under Enhanced Delirium
        Shadowstride = 36926, // Dash, basically never

    #endregion

    #region AoE oGCDs

        AbyssalDrain = 3641, // Cooldown shared with CarveAndSpit
        FloodOfDarkness = 16466, // For MP
        FloodOfShadow = 16469, // For MP // Upgrade of FloodOfDarkness
        Quietus = 7391, // For Blood
        SaltedEarth = 3639,
        SaltAndDarkness = 25755, // Recast of Salted Earth
        Impalement = 36931, // Under Delirium

    #endregion

    #region Buffing oGCDs

        BloodWeapon = 3625,
        Delirium = 7390,

    #endregion

    #region Burst Window

        LivingShadow = 16472,
        Shadowbringer = 25757,
        Disesteem = 36932,

    #endregion

    #region Ranged Option

        Unmend = 3624,

    #endregion

    #region Mitigation

        Grit =
            3629, // Lv10, instant, 2.0s CD (group 1), range 0, single-target, targets=Self
        ReleaseGrit =
            32067, // Lv10, instant, 1.0s CD (group 1), range 0, single-target, targets=Self
        ShadowWall =
            3636, // Lv38, instant, 120.0s CD (group 20), range 0, single-target, targets=Self
        DarkMind =
            3634, // Lv45, instant, 60.0s CD (group 8), range 0, single-target, targets=Self
        LivingDead =
            3638, // Lv50, instant, 300.0s CD (group 24), range 0, single-target, targets=Self
        DarkMissionary =
            16471, // Lv66, instant, 90.0s CD (group 14), range 0, AOE 30 circle, targets=Self
        BlackestNight =
            7393, // Lv70, instant, 15.0s CD (group 2), range 30, single-target, targets=Self/Party
        Oblation =
            25754, // Lv82, instant, 60.0s CD (group 18/71) (2 charges), range 30, single-target, targets=Self/Party
        ShadowedVigil =
            36927; // Lv92, instant, 120.0s CD (group 20), range 0, single-target, targets=Self, animLock=???

    #endregion

    #endregion

    public static class Buffs
    {
        #region Main Buffs

        /// Tank Stance
        public const ushort Grit = 743;

        /// The lowest level buff, before Delirium
        public const ushort BloodWeapon = 742;

        /// The lower Delirium buff, with just the blood ability usage
        public const ushort Delirium = 1972;

        /// Different from Delirium, to do the Scarlet Delirium chain
        public const ushort EnhancedDelirium = 3836;

        /// The increased damage buff that should always be up - checked through gauge
        public const ushort Darkside = 741;

        #endregion

        #region "DoT" or Burst

        /// Ground DoT active status
        public const ushort SaltedEarth = 749;

        /// Charge to be able to use Disesteem
        public const ushort Scorn = 3837;

        #endregion

        #region Mitigation

        /// TBN Active - Dark arts checked through gauge
        public const ushort BlackestNightShield = 1178;

        /// The initial Invuln that needs procc'd
        public const ushort LivingDead = 810;

        /// The real, triggered Invuln that gives heals
        public const ushort WalkingDead = 811;

        /// The Invuln after completely healed
        public const ushort UndeadRebirth = 3255;

        /// Damage Reduction part of Vigil
        public const ushort ShadowedVigil = 3835;

        /// The triggered part of Vigil that needs procc'd to heal (happens below 50%)
        public const ushort ShadowedVigilant = 3902;

        /// Oblation Active
        public const ushort Oblation = 2682;

        #endregion
    }

    public static class Traits
    {
        public const uint
            BloodWeaponMastery = 570,
            EnhancedDelirium = 572,
            EnhancedShadowIII = 573;
    }

    #endregion

    #region TryGet Setup

    /// <summary>
    ///     Flags to combine to provide to the `TryGet...Action` methods.
    /// </summary>
    [Flags]
    private enum Combo
    {
        // Target-type for combo
        ST = 1 << 0, // 1
        AoE = 1 << 1, // 2

        // Complexity of combo
        Adv = 1 << 2, // 4
        Simple = 1 << 3, // 8
    }

    private interface IActionProvider
    {
        bool TryGetAction(Combo flags, ref uint action, bool? extraParam = null);
    }

    /// <summary>
    ///     Checks whether a given preset is enabled, and the flags match it.
    /// </summary>
    private static bool IsSTEnabled(Combo flags, Preset preset) =>
        flags.HasFlag(Combo.ST) && IsEnabled(preset);

    /// <summary>
    ///     Checks whether a given preset is enabled, and the flags match it.
    /// </summary>
    private static bool IsAoEEnabled(Combo flags, Preset preset) =>
        flags.HasFlag(Combo.AoE) && IsEnabled(preset);

    /// <summary>
    ///     Signature for the TryGetAction&lt;ActionType&gt; methods.
    /// </summary>
    /// <param name="flags">
    ///     The flags to describe the combo executing this method.
    /// </param>
    /// <param name="action">The action to execute.</param>
    /// <param name="extraParam">Any extra parameter to pass through.</param>
    /// <returns>Whether the <c>action</c> was changed.</returns>
    /// <seealso cref="IActionProvider.TryGetAction" />
    /// <seealso cref="VariantAction" />
    /// <seealso cref="Mitigation" />
    /// <seealso cref="Spender" />
    /// <seealso cref="Cooldown" />
    /// <seealso cref="Core" />
    private static bool TryGetAction<T> (Combo flags, ref uint action, bool? extraParam = null)
        where T : IActionProvider, new() => new T().TryGetAction(flags, ref action, extraParam);

    #endregion
}
