﻿using Dalamud.Game.ClientState.Objects.Types;
using System.Collections.Generic;
using WrathCombo.Combos.PvE;
using WrathCombo.CustomComboNS;
using WrathCombo.CustomComboNS.Functions;
using static WrathCombo.CustomComboNS.Functions.CustomComboFunctions;

namespace WrathCombo.Combos.PvP
{
    internal static class PvPCommon
    {
        public const uint
            Teleport = 5,
            Return = 6,
            StandardElixir = 29055,
            Recuperate = 29711,
            Purify = 29056,
            Guard = 29054,
            Sprint = 29057;

        internal class Config
        {
            public static UserInt
                EmergencyHealThreshold = new("EmergencyHealThreshold"),
                EmergencyGuardThreshold = new("EmergencyGuardThreshold");
            public static UserBoolArray
                QuickPurifyStatuses = new("QuickPurifyStatuses");
        }

        internal class Debuffs
        {
            public const ushort
                Silence = 1347,
                Bind = 1345,
                Stun = 1343,
                HalfAsleep = 3022,
                Sleep = 1348,
                DeepFreeze = 3219,
                Heavy = 1344,
                Unguarded = 3021,
                MiracleOfNature = 3085;
        }

        internal class Buffs
        {
            public const ushort
                Sprint = 1342,
                Guard = 3054;
        }

        /// <summary> Checks if the target is immune to damage. Optionally, include buffs that provide significant damage reduction. </summary>
        /// <param name="includeReductions"> Includes buffs that provide significant damage reduction. </param>
        /// <param name="optionalTarget"> Optional target to check. </param>
        public static bool TargetImmuneToDamage(bool includeReductions = true, IGameObject? optionalTarget = null)
        {
            var t = optionalTarget ?? CurrentTarget;
            if (t is null || !InPvP()) return false;

            bool targetHasReductions = HasStatusEffect(Buffs.Guard, t, true) || HasStatusEffect(VPRPvP.Buffs.HardenedScales, t, true);
            bool targetHasImmunities = HasStatusEffect(DRKPvP.Buffs.UndeadRedemption, t, true) || HasStatusEffect(PLDPvP.Buffs.HallowedGround, t, true);

            return includeReductions
                ? targetHasReductions || targetHasImmunities
                : targetHasImmunities;
        }

        // Lists of Excluded skills 
        internal static readonly List<uint>
            MovmentSkills = [WARPvP.Onslaught, NINPvP.Shukuchi, DNCPvP.EnAvant, MNKPvP.ThunderClap, RDMPvP.CorpsACorps, RDMPvP.Displacement, SGEPvP.Icarus, RPRPvP.HellsIngress, RPRPvP.Regress, BRDPvP.RepellingShot, BLMPvP.AetherialManipulation, DRGPvP.ElusiveJump, GNBPvP.RoughDivide,
            GNBPvP.RelentlessRush, SAMPvP.Zantetsuken, RPRPvP.TenebraeLemurum, DRKPvP.Eventide, MCHPvP.MarksmanSpite, RDMPvP.SouthernCross, NINPvP.SeitonTenchu, NINPvP.Huton, NINPvP.Meisui, NINPvP.ThreeMudra, SGEPvP.Pneuma, SGEPvP.Mesotes, DRKPvP.BlackestNight,
            DRGPvP.HorridRoar, SAMPvP.Soten, SAMPvP.Chiten, MNKPvP.RiddleOfEarth, MNKPvP.EarthsReply, DNCPvP.CuringWaltz, DNCPvP.Contradance, PLDPvP.Phalanx, PLDPvP.HolySheltron, DRKPvP.Impalement, DRKPvP.SaltedEarth, DRKPvP.SaltAndDarkness, DRKPvP.Plunge, VPRPvP.Slither, VPRPvP.Backlash, VPRPvP.WorldSwallower, VPRPvP.SnakeScales, PCTPvP.Smudge, PCTPvP.HolyInWhite, PCTPvP.TemperaCoat, PCTPvP.StarPrism,
            PLDPvP.HolySpirit, PLDPvP.Guardian, PLDPvP.Intervene, WARPvP.Onslaught, WARPvP.PrimalRend, WARPvP.Bloodwhetting, WARPvP.Blota, WARPvP.PrimalScream, GNBPvP.RelentlessRush, GNBPvP.HeartOfCorundum, GNBPvP.FatedCircle, Recuperate, Sprint, Purify, StandardElixir, Teleport, RDMPvP.Forte, RDMPvP.Displacement, MCHPvP.BishopTurret, MCHPvP.Scattergun,
            BRDPvP.RepellingShot, SCHPvP.Expedient, SCHPvP.Aqloquilum, SMNPvP.CrimsonCyclone, SMNPvP.RadiantAegis, ASTPvP.Microcosmos, ASTPvP.Macrocosmos, WHMPvP.Cure2, WHMPvP.Cure3, WHMPvP.AfflatusPurgation, WHMPvP.Aquaveil, BLMPvP.Burst, DRGPvP.SkyHigh],

            GlobalSkills = [Teleport, Guard, Recuperate, Purify, StandardElixir, Sprint];

        internal class GlobalEmergencyHeals : CustomCombo
        {
            protected internal override CustomComboPreset Preset { get; } = CustomComboPreset.PvP_EmergencyHeals;

            protected override uint Invoke(uint actionID)
            {
                if ((HasStatusEffect(Buffs.Guard) || JustUsed(Guard)) && IsEnabled(CustomComboPreset.PvP_MashCancel))
                {
                    if (MovmentSkills.Contains(actionID))
                    {
                        return actionID; //allow for an exemption list
                    }
                    else
                        return OriginalHook(11); //execute the original action
                }

                if (Execute() &&
                     InPvP() &&
                    !GlobalSkills.Contains(actionID) &&
                    !MovmentSkills.Contains(actionID))
                    return OriginalHook(Recuperate);

                return actionID;
            }

            public static bool Execute()
            {
                var jobMaxHp = LocalPlayer.MaxHp;
                int threshold = Config.EmergencyHealThreshold;
                var maxHPThreshold = jobMaxHp - 15000;
                var remainingPercentage = (float)LocalPlayer.CurrentHp / (float)maxHPThreshold;


                if (HasStatusEffect(3180)) return false; //DRG LB buff
                if (HasStatusEffect(1420, anyOwner: true)) return false; //Rival Wings Mounted
                if (HasStatusEffect(4096)) return false; //VPR Snakesbane
                if (HasStatusEffect(DRKPvP.Buffs.UndeadRedemption)) return false;
                if (LocalPlayer.CurrentMp < 2500) return false;
                if (remainingPercentage * 100 > threshold) return false;

                return true;

            }
        }

        internal class GlobalEmergencyGuard : CustomCombo
        {
            protected internal override CustomComboPreset Preset { get; } = CustomComboPreset.PvP_EmergencyGuard;

            protected override uint Invoke(uint actionID)
            {
                if ((HasStatusEffect(Buffs.Guard) || JustUsed(Guard)) && IsEnabled(CustomComboPreset.PvP_MashCancel))
                {
                    if (MovmentSkills.Contains(actionID) || JustUsed(Guard))
                    {
                        return actionID; //allow for an exemption list
                    }

                    else if (actionID == Guard)
                    {

                        if (IsEnabled(CustomComboPreset.PvP_MashCancelRecup) && !JustUsed(Guard, 2f) && LocalPlayer.CurrentMp >= 2500 && LocalPlayer.CurrentHp <= LocalPlayer.MaxHp - 15000)
                            return Recuperate;
                        else
                            return Guard;
                    }
                    else
                        return OriginalHook(11);
                }

                if (Execute() &&
                    InPvP() &&
                    !GlobalSkills.Contains(actionID) &&
                    !MovmentSkills.Contains(actionID))
                    return All.SavageBlade;

                return actionID;
            }

            public static bool Execute()
            {
                var jobMaxHp = LocalPlayer.MaxHp;
                var threshold = Config.EmergencyGuardThreshold;
                var remainingPercentage = (float)LocalPlayer.CurrentHp / (float)jobMaxHp;

                if (HasStatusEffect(3180)) return false; //DRG LB buff
                if (HasStatusEffect(4096)) return false; //VPR Snakesbane
                if (HasStatusEffect(1420, anyOwner: true)) return false; //Rival Wings Mounted
                if (HasStatusEffect(DRKPvP.Buffs.UndeadRedemption)) return false;
                if (HasStatusEffect(Debuffs.Unguarded, anyOwner: true) || HasStatusEffect(WARPvP.Buffs.InnerRelease)) return false;
                if (GetCooldown(Guard).IsCooldown) return false;
                if (remainingPercentage * 100 > threshold) return false;

                return true;

            }
        }

        internal class QuickPurify : CustomCombo
        {
            protected internal override CustomComboPreset Preset { get; } = CustomComboPreset.PvP_QuickPurify;

            protected override uint Invoke(uint actionID)
            {
                if ((HasStatusEffect(Buffs.Guard) || JustUsed(Guard)) && IsEnabled(CustomComboPreset.PvP_MashCancel))
                {
                    if (MovmentSkills.Contains(actionID))
                    {
                        return actionID; //allow for an exemption list
                    }
                    else
                        return OriginalHook(11); //execute the original action
                }

                if (Execute() &&
                    InPvP() &&
                    !GlobalSkills.Contains(actionID))
                    return OriginalHook(Purify);

                return actionID;
            }

            public static bool Execute()
            {
                bool[] selectedStatuses = Config.QuickPurifyStatuses;

                if (HasStatusEffect(3180)) return false; //DRG LB buff
                if (HasStatusEffect(4096)) return false; //VPR Snakesbane
                if (HasStatusEffect(1420, anyOwner: true)) return false; //Rival Wings Mounted

                if (selectedStatuses.Length == 0) return false;
                if (GetCooldown(Purify).IsCooldown) return false;
                if (HasStatusEffect(Debuffs.Stun, anyOwner: true) && selectedStatuses[0]) return true;
                if (HasStatusEffect(Debuffs.DeepFreeze, anyOwner: true) && selectedStatuses[1]) return true;
                if (HasStatusEffect(Debuffs.HalfAsleep, anyOwner: true) && selectedStatuses[2]) return true;
                if (HasStatusEffect(Debuffs.Sleep, anyOwner: true) && selectedStatuses[3]) return true;
                if (HasStatusEffect(Debuffs.Bind, anyOwner: true) && selectedStatuses[4]) return true;
                if (HasStatusEffect(Debuffs.Heavy, anyOwner: true) && selectedStatuses[5]) return true;
                if (HasStatusEffect(Debuffs.Silence, anyOwner: true) && selectedStatuses[6]) return true;
                if (HasStatusEffect(Debuffs.MiracleOfNature, anyOwner: true) && selectedStatuses[7]) return true;

                return false;

            }
        }

    }

}
