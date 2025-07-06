﻿using Dalamud.Game.ClientState.JobGauge.Types;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.ClientState.Statuses;
using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using WrathCombo.CustomComboNS;
using WrathCombo.CustomComboNS.Functions;
using static WrathCombo.Combos.PvE.SGE.Config;
using static WrathCombo.CustomComboNS.Functions.CustomComboFunctions;
namespace WrathCombo.Combos.PvE;

internal partial class SGE
{
    internal static Status? DosisDebuff =>
        GetStatusEffect(DosisList[OriginalHook(Dosis)], CurrentTarget);

    internal static Status? DyskrasiaDebuff =>
        GetStatusEffect(Debuffs.EukrasianDyskrasia, CurrentTarget);

    internal static bool MaxPhlegma =>
        GetRemainingCharges(OriginalHook(Phlegma)) == GetMaxCharges(OriginalHook(Phlegma));

    internal static bool HasAddersgall() =>
        Addersgall > 0;

    internal static bool HasAddersting() =>
        Addersting > 0;

    #region Healing

    #region ST

    internal static int GetMatchingConfigST(int i, IGameObject? optionalTarget, out uint action, out bool enabled)
    {
        IGameObject? healTarget = optionalTarget ?? SimpleTarget.Stack.AllyToHeal;

        bool shieldCheck = !SGE_ST_Heal_EDiagnosisOpts[0] ||
                           !HasStatusEffect(Buffs.EukrasianDiagnosis, healTarget, true) ||
                           !HasStatusEffect(Buffs.EukrasianPrognosis, healTarget, true);

        bool scholarShieldCheck = !SGE_ST_Heal_EDiagnosisOpts[1] ||
                                  !HasStatusEffect(SCH.Buffs.Galvanize);

        switch (i)
        {
            case 0:
                action = Soteria;
                enabled = IsEnabled(CustomComboPreset.SGE_ST_Heal_Soteria);

                return SGE_ST_Heal_Soteria;

            case 1:
                action = Zoe;
                enabled = IsEnabled(CustomComboPreset.SGE_ST_Heal_Zoe);

                return SGE_ST_Heal_Zoe;

            case 2:
                action = Pepsis;

                enabled = IsEnabled(CustomComboPreset.SGE_ST_Heal_Pepsis) &&
                          HasStatusEffect(Buffs.EukrasianDiagnosis, healTarget);

                return SGE_ST_Heal_Pepsis;

            case 3:
                action = Taurochole;
                enabled = IsEnabled(CustomComboPreset.SGE_ST_Heal_Taurochole) && HasAddersgall();

                return SGE_ST_Heal_Taurochole;

            case 4:
                action = Haima;
                enabled = IsEnabled(CustomComboPreset.SGE_ST_Heal_Haima);

                return SGE_ST_Heal_Haima;

            case 5:
                action = Krasis;
                enabled = IsEnabled(CustomComboPreset.SGE_ST_Heal_Krasis);

                return SGE_ST_Heal_Krasis;

            case 6:
                action = Druochole;
                enabled = IsEnabled(CustomComboPreset.SGE_ST_Heal_Druochole) && HasAddersgall();

                return SGE_ST_Heal_Druochole;

            case 7:
                action = Eukrasia;
                enabled = IsEnabled(CustomComboPreset.SGE_ST_Heal_EDiagnosis) &&
                          GetTargetHPPercent(healTarget, SGE_ST_Heal_IncludeShields) <= SGE_ST_Heal_EDiagnosisHP &&
                          shieldCheck && scholarShieldCheck;

                return SGE_ST_Heal_EDiagnosisHP;
        }

        enabled = false;
        action = 0;

        return 0;
    }

    #endregion

    #region AoE

    internal static int GetMatchingConfigAoE(int i, out uint action, out bool enabled)
    {
        bool shieldCheck = GetPartyBuffPercent(Buffs.EukrasianPrognosis) <= SGE_AoE_Heal_EPrognosisOption &&
                           GetPartyBuffPercent(SCH.Buffs.Galvanize) <= SGE_AoE_Heal_EPrognosisOption;

        bool anyPanhaima = !SGE_ST_Heal_PanhaimaOpts[0] ||
                           !HasStatusEffect(Buffs.Panhaima, null, true);
        switch (i)
        {
            case 0:
                action = Kerachole;
                enabled = IsEnabled(CustomComboPreset.SGE_AoE_Heal_Kerachole) &&
                          (!SGE_AoE_Heal_KeracholeTrait ||
                           SGE_AoE_Heal_KeracholeTrait && TraitLevelChecked(Traits.EnhancedKerachole)) &&
                          HasAddersgall();
                return SGE_AoE_Heal_KeracholeOption;

            case 1:
                action = Ixochole;
                enabled = IsEnabled(CustomComboPreset.SGE_AoE_Heal_Ixochole) &&
                          HasAddersgall();
                return SGE_AoE_Heal_IxocholeOption;

            case 2:
                action = OriginalHook(Physis);
                enabled = IsEnabled(CustomComboPreset.SGE_AoE_Heal_Physis);
                return SGE_AoE_Heal_PhysisOption;

            case 3:
                action = Holos;
                enabled = IsEnabled(CustomComboPreset.SGE_AoE_Heal_Holos);
                return SGE_AoE_Heal_HolosOption;

            case 4:
                action = Panhaima;
                enabled = IsEnabled(CustomComboPreset.SGE_AoE_Heal_Panhaima) && anyPanhaima;
                return SGE_AoE_Heal_PanhaimaOption;

            case 5:
                action = Pepsis;
                enabled = IsEnabled(CustomComboPreset.SGE_AoE_Heal_Pepsis) &&
                          HasStatusEffect(Buffs.EukrasianPrognosis);
                return SGE_AoE_Heal_PepsisOption;

            case 6:
                action = Philosophia;
                enabled = IsEnabled(CustomComboPreset.SGE_AoE_Heal_Philosophia);
                return SGE_AoE_Heal_PhilosophiaOption;

            case 7:
                action = Zoe;
                enabled = IsEnabled(CustomComboPreset.SGE_AoE_Heal_Zoe);
                return SGE_AoE_Heal_ZoeOption;

            case 8:
                action = Eukrasia;
                enabled = IsEnabled(CustomComboPreset.SGE_AoE_Heal_EPrognosis)
                          && shieldCheck;
                return 100; //Don't HP Check
        }

        enabled = false;
        action = 0;
        return 0;
    }

    #endregion

    #endregion

    #region Movement Prio

    private static (uint Action, CustomComboPreset Preset, System.Func<bool> Logic)[]
        PrioritizedMovement =>
    [
        //Toxikon
        (OriginalHook(Toxikon), CustomComboPreset.SGE_ST_DPS_Movement,
            () => SGE_ST_DPS_Movement[0] &&
                  ActionReady(Toxikon) &&
                  HasAddersting()),
        // Dyskrasia
        (OriginalHook(Dyskrasia), CustomComboPreset.SGE_ST_DPS_Movement,
            () => SGE_ST_DPS_Movement[1] &&
                  ActionReady(Dyskrasia) &&
                  InActionRange(Dyskrasia)),
        //Eukrasia
        (Eukrasia, CustomComboPreset.SGE_ST_DPS_Movement,
            () => SGE_ST_DPS_Movement[2] &&
                  ActionReady(Eukrasia) &&
                  !HasStatusEffect(Buffs.Eukrasia))
    ];

    private static bool CheckMovementConfigMeetsRequirements
        (int index, out uint action)
    {
        action = PrioritizedMovement[index].Action;
        return ActionReady(action) && LevelChecked(action) &&
               PrioritizedMovement[index].Logic() &&
               IsEnabled(PrioritizedMovement[index].Preset);
    }

    #endregion

    #region Openers

    internal static WrathOpener Opener()
    {
        if (ToxikonOpener.LevelChecked &&
            SGE_SelectedOpener == 0)
            return ToxikonOpener;

        if (PneumaOpener.LevelChecked &&
            SGE_SelectedOpener == 1)
            return PneumaOpener;

        return WrathOpener.Dummy;
    }

    internal static SGEToxikonOpener ToxikonOpener = new();
    internal static SGEPneumaOpener PneumaOpener = new();

    internal class SGEToxikonOpener : WrathOpener
    {
        public override int MinOpenerLevel => 92;

        public override int MaxOpenerLevel => 109;

        public override List<uint> OpenerActions { get; set; } =
        [
            Toxikon2,
            Eukrasia,
            EukrasianDosis3,
            Dosis3,
            Dosis3,
            Dosis3,
            Phlegma3,
            Psyche,
            Phlegma3,
            Dosis3,
            Dosis3,
            Dosis3,
            Dosis3,
            Eukrasia,
            EukrasianDosis3,
            Dosis3,
            Dosis3,
            Dosis3
        ];

        public override List<(int[] Steps, Func<bool> Condition)> SkipSteps { get; set; } =
        [
            ([2], () => HasStatusEffect(Buffs.Eukrasia))
        ];

        internal override UserData ContentCheckConfig => SGE_Balance_Content;

        public override bool HasCooldowns() =>
            GetRemainingCharges(Phlegma3) is 2 &&
            IsOffCooldown(Psyche) &&
            HasAddersting();
    }

    internal class SGEPneumaOpener : WrathOpener
    {
        public override int MinOpenerLevel => 92;

        public override int MaxOpenerLevel => 109;

        public override List<uint> OpenerActions { get; set; } =
        [
            Pneuma,
            Eukrasia,
            EukrasianDosis3,
            Dosis3,
            Dosis3,
            Dosis3,
            Phlegma3,
            Psyche,
            Phlegma3,
            Dosis3,
            Dosis3,
            Dosis3,
            Dosis3,
            Eukrasia,
            EukrasianDosis3,
            Dosis3,
            Dosis3,
            Dosis3
        ];

        public override List<(int[] Steps, Func<bool> Condition)> SkipSteps { get; set; } =
        [
            ([2], () => HasStatusEffect(Buffs.Eukrasia))
        ];


        internal override UserData ContentCheckConfig => SGE_Balance_Content;

        public override bool HasCooldowns() =>
            GetRemainingCharges(Phlegma3) is 2 &&
            IsOffCooldown(Psyche) &&
            IsOffCooldown(Pneuma);
    }

    #endregion

    #region Gauge

    internal static SGEGauge Gauge = GetJobGauge<SGEGauge>();

    internal static byte Addersgall => Gauge.Addersgall;

    internal static byte Addersting => Gauge.Addersting;

    internal static readonly List<uint>
        AddersgallList = [Taurochole, Druochole, Ixochole, Kerachole],
        DyskrasiaList = [Dyskrasia, Dyskrasia2];

    internal static readonly FrozenDictionary<uint, ushort> DosisList = new Dictionary<uint, ushort>
    {
        { Dosis, Debuffs.EukrasianDosis },
        { Dosis2, Debuffs.EukrasianDosis2 },
        { Dosis3, Debuffs.EukrasianDosis3 },
        { EukrasianDosis, Debuffs.EukrasianDosis },
        { EukrasianDosis2, Debuffs.EukrasianDosis2 },
        { EukrasianDosis3, Debuffs.EukrasianDosis3 }
    }.ToFrozenDictionary();

    #endregion

    #region ID's

    internal const byte JobID = 40;

    // Actions
    internal const uint

        // Heals and Shields
        Diagnosis = 24284,
        Prognosis = 24286,
        Physis = 24288,
        Druochole = 24296,
        Kerachole = 24298,
        Ixochole = 24299,
        Pepsis = 24301,
        Physis2 = 24302,
        Taurochole = 24303,
        Haima = 24305,
        Panhaima = 24311,
        Holos = 24310,
        EukrasianDiagnosis = 24291,
        EukrasianPrognosis = 24292,
        Egeiro = 24287,

        // DPS
        Dosis = 24283,
        Dosis2 = 24306,
        Dosis3 = 24312,
        EukrasianDosis = 24293,
        EukrasianDosis2 = 24308,
        EukrasianDosis3 = 24314,
        Phlegma = 24289,
        Phlegma2 = 24307,
        Phlegma3 = 24313,
        Dyskrasia = 24297,
        Dyskrasia2 = 24315,
        Toxikon = 24304,
        Toxikon2 = 24316,
        Pneuma = 24318,
        EukrasianDyskrasia = 37032,
        Psyche = 37033,

        // Buffs
        Soteria = 24294,
        Zoe = 24300,
        Krasis = 24317,
        Philosophia = 37035,

        // Other
        Kardia = 24285,
        Eukrasia = 24290,
        Rhizomata = 24309;

    // Action Groups


    // Debuff Pairs of Actions and Debuff


    // Action Buffs
    internal static class Buffs
    {
        internal const ushort
            Kardia = 2604,
            Kardion = 2605,
            Eukrasia = 2606,
            EukrasianDiagnosis = 2607,
            EukrasianPrognosis = 2609,
            Panhaima = 2613,
            Kerachole = 2618,
            Zoe = 2611,
            Eudaimonia = 3899;
    }

    internal static class Debuffs
    {
        internal const ushort
            EukrasianDosis = 2614,
            EukrasianDosis2 = 2615,
            EukrasianDosis3 = 2616,
            EukrasianDyskrasia = 3897;
    }

    internal static class Traits
    {
        internal const ushort
            EnhancedKerachole = 375,
            OffensiveMagicMasteryII = 376;
    }

    #endregion
}
