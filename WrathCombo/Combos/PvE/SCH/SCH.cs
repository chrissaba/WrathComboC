using System.Linq;
using WrathCombo.Core;
using WrathCombo.CustomComboNS;
using WrathCombo.Data;
using WrathCombo.Extensions;
namespace WrathCombo.Combos.PvE;

internal partial class SCH : Healer
{
    #region ST DPS
    internal class SCH_DPS : CustomCombo
    {
        protected internal override CustomComboPreset Preset { get; } = CustomComboPreset.SCH_DPS;
        internal static int BroilCount => ActionWatching.CombatActions.Count(x => x == OriginalHook(Broil));
        protected override uint Invoke(uint actionID)
        {
            #region Determine Replaced Action
            bool actionFound;
            if (Config.SCH_ST_DPS_Adv && Config.SCH_ST_DPS_Adv_Actions.Count > 0)
            {
                bool onBroils = Config.SCH_ST_DPS_Adv_Actions[0] && BroilList.Contains(actionID);
                bool onBios = Config.SCH_ST_DPS_Adv_Actions[1] && BioList.ContainsKey(actionID);
                bool onRuinII = Config.SCH_ST_DPS_Adv_Actions[2] && actionID is Ruin2;
                actionFound = onBroils || onBios || onRuinII;
            }
            else
                actionFound = BroilList.Contains(actionID); //default handling
            #endregion
            
            if (!actionFound)
                return actionID;

            if (IsEnabled(CustomComboPreset.SCH_DPS_FairyReminder) && NeedToSummon)
                return SummonEos;
            //Opener
            if (IsEnabled(CustomComboPreset.SCH_DPS_Balance_Opener) && Opener().FullOpener(ref actionID))
                return actionID;
            
            #region Variant
            if (Variant.CanRampart(CustomComboPreset.SCH_DPS_Variant_Rampart))
                return Variant.Rampart;
            
            if (Variant.CanSpiritDart(CustomComboPreset.SCH_DPS_Variant_SpiritDart))
                return Variant.SpiritDart;

            if (OccultCrescent.ShouldUsePhantomActions())
                return OccultCrescent.BestPhantomAction();
            #endregion
            
            #region Dissolve Union
            if (EndAetherpact)
                return DissolveUnion;
            #endregion
            
            #region Hidden Feature Raidwide

            if (RaidWideCasting())
                return HiddenRaidwides(actionID);
           
            #endregion

            if (InCombat() && CanSpellWeave())
            {
                // Aetherflow
                if (IsEnabled(CustomComboPreset.SCH_DPS_Aetherflow) && !WasLastAction(Dissipation) && ActionReady(Aetherflow) && !HasAetherflow)
                    return Aetherflow;
                
                if (IsEnabled(CustomComboPreset.SCH_DPS_BanefulImpact) && HasStatusEffect(Buffs.ImpactImminent) && !JustUsed(ChainStratagem))
                    return BanefulImpaction;
                
                if (IsEnabled(CustomComboPreset.SCH_DPS_ChainStrat) && ActionWatching.NumberOfGcdsUsed > 3 && ActionReady(ChainStratagem) && 
                    CanApplyStatus(CurrentTarget, Debuffs.ChainStratagem) &&
                    !HasStatusEffect(Debuffs.ChainStratagem, CurrentTarget, true) &&
                    GetTargetHPPercent() > Config.SCH_ST_DPS_ChainStratagemOption &&
                    (Config.SCH_ST_DPS_ChainStratagemSubOption == 0 || Config.SCH_ST_DPS_ChainStratagemSubOption == 1 && InBossEncounter()))
                    return ChainStratagem;
                    
                if (IsEnabled(CustomComboPreset.SCH_DPS_EnergyDrain) && ActionReady(EnergyDrain) && 
                    GetCooldownRemainingTime(Aetherflow) <= Config.SCH_ST_DPS_EnergyDrain &&
                    (!IsEnabled(CustomComboPreset.SCH_DPS_EnergyDrain_BurstSaver) ||
                     GetCooldownRemainingTime(ChainStratagem) > 10 ||
                     !LevelChecked(ChainStratagem)))
                    return EnergyDrain;
                
                if (IsEnabled(CustomComboPreset.SCH_DPS_Lucid) && Role.CanLucidDream(Config.SCH_ST_DPS_LucidOption))
                    return Role.LucidDreaming;
            }
            
            //Bio/Biolysis
            if (IsEnabled(CustomComboPreset.SCH_DPS_Bio) && NeedsDoT() && InCombat())
                return OriginalHook(Bio);

            //Ruin 2 Movement
            if (IsEnabled(CustomComboPreset.SCH_DPS_Ruin2Movement) && ActionReady(Ruin2) && IsMoving() && InCombat())
                return OriginalHook(Ruin2);
            
            return actionID;
        }
    }
    #endregion
    
    #region AoE DPS
    internal class SCH_AoE : CustomCombo
    {
        protected internal override CustomComboPreset Preset { get; } = CustomComboPreset.SCH_AoE;
        protected override uint Invoke(uint actionID)
        {
            if (actionID is not (ArtOfWar or ArtOfWarII))
                return actionID;

            if (IsEnabled(CustomComboPreset.SCH_AoE_FairyReminder) &&
                NeedToSummon)
                return SummonEos;
            
            #region Variant
            if (Variant.CanRampart(CustomComboPreset.SCH_DPS_Variant_Rampart))
                return Variant.Rampart;

            if (Variant.CanSpiritDart(CustomComboPreset.SCH_DPS_Variant_SpiritDart))
                return Variant.SpiritDart;

            if (OccultCrescent.ShouldUsePhantomActions())
                return OccultCrescent.BestPhantomAction();
            #endregion
            
            #region Dissolve Union
            if (EndAetherpact)
                return DissolveUnion;
            #endregion
            
            #region Hidden Feature Raidwide

            if (RaidWideCasting())
                return HiddenRaidwides(actionID);
           
            #endregion
            
            if (!InCombat() || !CanSpellWeave()) return actionID;
            
            if (IsEnabled(CustomComboPreset.SCH_AoE_Aetherflow) && !WasLastAction(Dissipation) && ActionReady(Aetherflow) && !HasAetherflow)
                return Aetherflow;
                
            if (IsEnabled(CustomComboPreset.SCH_AoE_BanefulImpact) && HasStatusEffect(Buffs.ImpactImminent) && !JustUsed(ChainStratagem))
                return BanefulImpaction;
                
            if (IsEnabled(CustomComboPreset.SCH_AoE_ChainStrat) && ActionWatching.NumberOfGcdsUsed > 3 && ActionReady(ChainStratagem) && 
                (LevelChecked(BanefulImpaction) && IsEnabled(CustomComboPreset.SCH_AoE_BanefulImpact) || !IsEnabled(CustomComboPreset.SCH_AoE_ChainStrat_BanefulOnly)) &&
                CanApplyStatus(CurrentTarget, Debuffs.ChainStratagem) && 
                !HasStatusEffect(Debuffs.ChainStratagem, CurrentTarget, true) &&
                GetTargetHPPercent() > Config.SCH_AoE_DPS_ChainStratagemOption &&
                (Config.SCH_AoE_DPS_ChainStratagemSubOption == 0 || Config.SCH_AoE_DPS_ChainStratagemSubOption == 1 && InBossEncounter()))
                return ChainStratagem;
                    
            if (IsEnabled(CustomComboPreset.SCH_AoE_EnergyDrain) && ActionReady(EnergyDrain) && 
                GetCooldownRemainingTime(Aetherflow) <= Config.SCH_AoE_DPS_EnergyDrain &&
                (!IsEnabled(CustomComboPreset.SCH_AoE_EnergyDrain_BurstSaver) ||
                 GetCooldownRemainingTime(ChainStratagem) > 10 ||
                 !LevelChecked(ChainStratagem)))
                return EnergyDrain;
                
            if (IsEnabled(CustomComboPreset.SCH_AoE_Lucid) && Role.CanLucidDream(Config.SCH_AoE_DPS_LucidOption))
                return Role.LucidDreaming;

            return actionID;
        }
    }

    #endregion
    
    #region ST Heal
    internal class SCH_ST_Heal : CustomCombo
    {
        protected internal override CustomComboPreset Preset { get; } = CustomComboPreset.SCH_ST_Heal;
        protected override uint Invoke(uint actionID)
        {
            if (actionID is not Physick)
                return actionID;
            
            #region Variables
            var healTarget = OptionalTarget ?? SimpleTarget.Stack.AllyToHeal;
            #endregion
            
            #region Priority Cleansing
            
            if (IsEnabled(CustomComboPreset.SCH_ST_Heal_Esuna) && 
                ActionReady(Role.Esuna) && HasCleansableDebuff(healTarget) &&
                GetTargetHPPercent(healTarget, Config.SCH_ST_Heal_IncludeShields) >= Config.SCH_ST_Heal_EsunaOption)
                return Role.Esuna
                    .RetargetIfEnabled(OptionalTarget, Physick);

            #endregion
            
            #region Dissolve Union
            if (EndAetherpact)
                return DissolveUnion;
            #endregion
            
            #region Hidden Feature Raidwide

            if (RaidWideCasting())
                return HiddenRaidwides(actionID);
           
            #endregion
            
            // Aetherflow
            if (IsEnabled(CustomComboPreset.SCH_ST_Heal_Aetherflow) &&
                ActionReady(Aetherflow) && !HasAetherflow &&
                InCombat() && CanSpellWeave())
                return Aetherflow;

            // Dissipation
            if (IsEnabled(CustomComboPreset.SCH_ST_Heal_Dissipation)
                && ActionReady(Dissipation)
                && !HasAetherflow
                && InCombat()
                && !FairyBusy)
                return Dissipation;

            // Lucid Dreaming
            if (IsEnabled(CustomComboPreset.SCH_ST_Heal_Lucid) &&
                Role.CanLucidDream(Config.SCH_ST_Heal_LucidOption))
                return Role.LucidDreaming;

            //Priority List
            for(int i = 0; i < Config.SCH_ST_Heals_Priority.Count; i++)
            {
                int index = Config.SCH_ST_Heals_Priority.IndexOf(i + 1);
                int config = GetMatchingConfigST(index, OptionalTarget, out uint spell, out bool enabled);

                if (enabled)
                {
                    if (Config.SCH_ST_Heal_AldoquimOpts[2] && ActionReady(OriginalHook(EmergencyTactics)) &&
                        spell is Adloquium or Manifestation && 
                        GetTargetHPPercent(healTarget, Config.SCH_ST_Heal_IncludeShields) <=
                        Config.SCH_ST_Heal_AdloquiumOption_Emergency)
                        return OriginalHook(EmergencyTactics);
                    
                    if (GetTargetHPPercent(healTarget, Config.SCH_ST_Heal_IncludeShields) <= config &&
                        ActionReady(spell))
                        return spell.RetargetIfEnabled(OptionalTarget, Physick);
                }
            }
            return actionID
                .RetargetIfEnabled(OptionalTarget, Physick);
        }
    }
    #endregion
    
    #region Aoe Heal 
    internal class SCH_AoE_Heal : CustomCombo
    {
        protected internal override CustomComboPreset Preset { get; } = CustomComboPreset.SCH_AoE_Heal;
        protected override uint Invoke(uint actionID)
        {
            if (actionID is not (Succor or Concitation or Accession))
                return actionID;
            
            #region Dissolve Union
            if (EndAetherpact)
                return DissolveUnion;
            #endregion
            
            #region Hidden Feature Raidwide

            if (RaidWideCasting())
                return HiddenRaidwides(actionID);
           
            #endregion

            if (!HasAetherflow && InCombat())
            {
                if (IsEnabled(CustomComboPreset.SCH_AoE_Heal_Aetherflow) && ActionReady(Aetherflow) && 
                    (!IsEnabled(CustomComboPreset.SCH_AoE_Heal_Aetherflow_Indomitability) || GetCooldownRemainingTime(Indomitability) <= 1))
                    return Aetherflow;

                if (IsEnabled(CustomComboPreset.SCH_AoE_Heal_Dissipation) && ActionReady(Dissipation) && !FairyBusy &&
                    (!IsEnabled(CustomComboPreset.SCH_AoE_Heal_Dissipation_Indomitability) || GetCooldownRemainingTime(Indomitability) <= 1))
                    return Dissipation;
            }
            if (IsEnabled(CustomComboPreset.SCH_AoE_Heal_Lucid) && Role.CanLucidDream(Config.SCH_AoE_Heal_LucidOption))
                return Role.LucidDreaming;

            //Priority List
            float averagePartyHP = GetPartyAvgHPPercent();
            for(int i = 0; i < Config.SCH_AoE_Heals_Priority.Count; i++)
            {
                int index = Config.SCH_AoE_Heals_Priority.IndexOf(i + 1);
                int config = GetMatchingConfigAoE(index, out uint spell, out bool enabled);
                bool onIdom = Config.SCH_AoE_Heal_Indomitability_Recitation && spell is Indomitability;
                bool onSuccor = Config.SCH_AoE_Heal_Succor_Options[1] && spell is Succor or Concitation or Accession;

                if (enabled && averagePartyHP <= config && ActionReady(spell))
                     return ActionReady(Recitation) && (onIdom || onSuccor) ? 
                        Recitation :
                        spell;
            }

            if (Config.SCH_AoE_Heal_Succor_Options[0] && ActionReady(EmergencyTactics))
                return OriginalHook(EmergencyTactics);
            
            return !LevelChecked(Succor)?
                WhisperingDawn:
                actionID;
        }
    }
    
    #endregion
    
    #region Standalone Features
    
    #region Sacred Soil Retarget
    internal class SCH_SacredSoil : CustomCombo
    {
        protected internal override CustomComboPreset Preset { get; } = CustomComboPreset.SCH_SacredSoil;
        protected override uint Invoke(uint actionID)
        {
            if (actionID is not SacredSoil)
                return actionID;
            
            var sacredSoilTarget =
                    (IsEnabled(CustomComboPreset.SCH_SacredSoil_Enemy)
                        ? SimpleTarget.HardTarget
                        : null) ??
                    (IsEnabled(CustomComboPreset.SCH_SacredSoil_Allies)
                        ? SimpleTarget.Stack.OverridesAllies
                        : null) ??
                    SimpleTarget.Self;
                
            return IsEnabled(CustomComboPreset.SCH_SacredSoil)? SacredSoil.Retarget(sacredSoilTarget) : actionID;
        }
    }
    #endregion
    
    #region Fey Blessing to Consolation
    internal class SCH_Consolation : CustomCombo
    {
        protected internal override CustomComboPreset Preset { get; } = CustomComboPreset.SCH_Consolation;
        protected override uint Invoke(uint actionID)
            => actionID is FeyBlessing && LevelChecked(SummonSeraph) && Gauge.SeraphTimer > 0 ? Consolation : actionID;
    }
    #endregion
    
    #region Lustrate to Excogitation
    internal class SCH_Lustrate : CustomCombo
    {
        protected internal override CustomComboPreset Preset { get; } = CustomComboPreset.SCH_Lustrate;
        protected override uint Invoke(uint actionID) =>
            actionID is Lustrate &&
            LevelChecked(Excogitation) && IsOffCooldown(Excogitation)
                ? Excogitation
                : actionID;
    }
    #endregion
    
    #region Recitation to Selected Option
    internal class SCH_Recitation : CustomCombo
    {
        protected internal override CustomComboPreset Preset { get; } = CustomComboPreset.SCH_Recitation;
        protected override uint Invoke(uint actionID)
        {
            if (actionID is not Recitation || !HasStatusEffect(Buffs.Recitation))
                return actionID;

            switch ((int)Config.SCH_Recitation_Mode)
            {
                case 0: return OriginalHook(Adloquium);
                case 1: return OriginalHook(Succor);
                case 2: return OriginalHook(Indomitability);
                case 3: return OriginalHook(Excogitation);
            }

            return actionID;
        }
    }
    #endregion

    #region Aetherflow Reminder
    internal class SCH_Aetherflow : CustomCombo
    {
        protected internal override CustomComboPreset Preset { get; } = CustomComboPreset.SCH_Aetherflow;
        protected override uint Invoke(uint actionID)
        {
            if (!AetherflowList.Contains(actionID) || !LevelChecked(Aetherflow))
                return actionID;

            bool hasAetherFlows = HasAetherflow; //False if Zero stacks

            if (IsEnabled(CustomComboPreset.SCH_Aetherflow_Recite) &&
                LevelChecked(Recitation) &&
                (IsOffCooldown(Recitation) || HasStatusEffect(Buffs.Recitation)))
            {
                //Recitation Indominability and Excogitation, with optional check against AF zero stack count
                bool alwaysShowReciteExcog = Config.SCH_Aetherflow_Recite_ExcogMode == 1;

                if (Config.SCH_Aetherflow_Recite_Excog &&
                    (alwaysShowReciteExcog ||
                     !alwaysShowReciteExcog && !hasAetherFlows) && actionID is Excogitation)
                {
                    //Do not merge this nested if with above. Won't procede with next set
                    return HasStatusEffect(Buffs.Recitation) && IsOffCooldown(Excogitation)
                        ? Excogitation
                        : Recitation;
                }

                bool alwaysShowReciteIndom = Config.SCH_Aetherflow_Recite_IndomMode == 1;

                if (Config.SCH_Aetherflow_Recite_Indom &&
                    (alwaysShowReciteIndom ||
                     !alwaysShowReciteIndom && !hasAetherFlows) && actionID is Indomitability)
                {
                    //Same as above, do not nest with above. It won't procede with the next set
                    return HasStatusEffect(Buffs.Recitation) && IsOffCooldown(Excogitation)
                        ? Indomitability
                        : Recitation;
                }
            }
            if (!hasAetherFlows)
            {
                bool showAetherflowOnAll = Config.SCH_Aetherflow_Display == 1;

                if ((actionID is EnergyDrain && !showAetherflowOnAll || showAetherflowOnAll) &&
                    IsOffCooldown(actionID))
                {
                    if (IsEnabled(CustomComboPreset.SCH_Aetherflow_Dissipation) &&
                        ActionReady(Dissipation) && IsOnCooldown(Aetherflow) && HasPetPresent())
                        //Dissipation requires fairy, can't seem to make it replace dissipation with fairy summon feature *shrug*
                        return Dissipation;

                    return Aetherflow;
                }
            }
            return actionID;
        }
    }
    #endregion
    
    #region Swiftcast to Raise
    internal class SCH_Raise : CustomCombo
    {
        protected internal override CustomComboPreset Preset { get; } = CustomComboPreset.SCH_Raise;
        protected override uint Invoke(uint actionID) =>
            actionID == Role.Swiftcast && IsOnCooldown(Role.Swiftcast)
                ? IsEnabled(CustomComboPreset.SCH_Raise_Retarget)
                    ? Resurrection.Retarget(Role.Swiftcast,
                        SimpleTarget.Stack.AllyToRaise)
                    : Resurrection
                : actionID;
    }
    #endregion
    
    #region Fairy Reminder
    internal class SCH_FairyReminder : CustomCombo
    {
        protected internal override CustomComboPreset Preset { get; } = CustomComboPreset.SCH_FairyReminder;
        protected override uint Invoke(uint actionID)
            => FairyList.Contains(actionID) && NeedToSummon ? SummonEos : actionID;
    }
    #endregion
    
    #region Deployment Tactics to Adloquium
    internal class SCH_DeploymentTactics : CustomCombo
    {
        protected internal override CustomComboPreset Preset { get; } = CustomComboPreset.SCH_DeploymentTactics;
        protected override uint Invoke(uint actionID)
        {
            if (actionID is not DeploymentTactics || !ActionReady(DeploymentTactics))
                return actionID;

            //Grab our target
            var healTarget = OptionalTarget ?? SimpleTarget.Stack.AllyToHeal;

            //Check for the Galvanize shield buff. Start applying if it doesn't exist
            if (!HasStatusEffect(Buffs.Galvanize, healTarget)) 
            {
                if (IsEnabled(CustomComboPreset.SCH_DeploymentTactics_Recitation) && ActionReady(Recitation))
                    return Recitation;

                return OriginalHook(Adloquium);
            }
            return actionID;
        }
    }
    #endregion
    
    #region Whispering Dawn to Fairy Abilities
    internal class SCH_Fairy_Combo : CustomCombo
    {
        protected internal override CustomComboPreset Preset { get; } = CustomComboPreset.SCH_Fairy_Combo;
        protected override uint Invoke(uint actionID)
        {
            if (actionID is not WhisperingDawn)
                return actionID;

            if (HasPetPresent())
            {
                // FeyIllumination
                if (ActionReady(FeyIllumination))
                    return OriginalHook(FeyIllumination);

                // FeyBlessing
                if (ActionReady(FeyBlessing) && !(Gauge.SeraphTimer > 0))
                    return OriginalHook(FeyBlessing);

                if (IsEnabled(CustomComboPreset.SCH_Fairy_Combo_Consolation) && ActionReady(WhisperingDawn))
                    return OriginalHook(actionID);

                if (IsEnabled(CustomComboPreset.SCH_Fairy_Combo_Consolation) && Gauge.SeraphTimer > 0 && GetRemainingCharges(Consolation) > 0)
                    return OriginalHook(Consolation);
            }
            return actionID;
        }
    }
    #endregion
    
    #endregion
}
