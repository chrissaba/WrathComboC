﻿using Dalamud.Interface.Colors;
using ImGuiNET;
using WrathCombo.CustomComboNS.Functions;
using WrathCombo.Data;
using static WrathCombo.Extensions.UIntExtensions;
using static WrathCombo.Window.Functions.SliderIncrements;
using static WrathCombo.Window.Functions.UserConfig;
namespace WrathCombo.Combos.PvE;

internal partial class SCH
{
    internal static class Config
    {
        internal static void Draw(CustomComboPreset preset)
        {
            switch (preset)
            {
                #region DPS
                case CustomComboPreset.SCH_DPS_Balance_Opener:
                    DrawHorizontalRadioButton(SCH_ST_DPS_OpenerOption, "Dissipation First", "Uses Dissipation first, then Aetherflow", 0);
                    DrawHorizontalRadioButton(SCH_ST_DPS_OpenerOption, "Aetherflow First", "Uses Aetherflow first, then Dissipation", 1);
                    DrawBossOnlyChoice(SCH_ST_DPS_OpenerContent);
                    break;

                case CustomComboPreset.SCH_DPS:
                    DrawAdditionalBoolChoice(SCH_ST_DPS_Adv, "Advanced Action Options", "Change how actions are handled", isConditionalChoice: true);
                    if (SCH_ST_DPS_Adv)
                    {
                        ImGui.Indent();
                        ImGui.Spacing();
                        DrawHorizontalMultiChoice(SCH_ST_DPS_Adv_Actions, "On Ruin/Broils", "Apply options to Ruin and all Broils.", 3, 0);
                        DrawHorizontalMultiChoice(SCH_ST_DPS_Adv_Actions, "On Bio/Bio II/Biolysis", "Apply options to Bio and Biolysis.", 3, 1);
                        DrawHorizontalMultiChoice(SCH_ST_DPS_Adv_Actions, "On Ruin II", "Apply options to Ruin II.", 3, 2);
                        ImGui.Unindent();
                    }
                    break;
                
                case CustomComboPreset.SCH_DPS_Lucid:
                    DrawSliderInt(4000, 9500, SCH_ST_DPS_LucidOption, "MP Threshold", 150, Hundreds);
                    break;

                case CustomComboPreset.SCH_DPS_Bio:

                    DrawSliderInt(0, 50, SCH_DPS_BioOption, "Stop using at Enemy HP %. Set to Zero to disable this check.");

                    ImGui.Indent();

                    ImGui.TextColored(ImGuiColors.DalamudYellow, "Select what kind of enemies the HP check should be applied to:");

                    DrawHorizontalRadioButton(SCH_DPS_BioSubOption,
                        "Non-Bosses", "Only applies the HP check above to non-bosses.\nAllows you to only stop DoTing early when it's not a boss.", 0);

                    DrawHorizontalRadioButton(SCH_DPS_BioSubOption,
                        "All Enemies", "Applies the HP check above to all enemies.", 1);

                    DrawRoundedSliderFloat(0, 4, SCH_DPS_BioUptime_Threshold, "Seconds remaining before reapplying the DoT. Set to Zero to disable this check.", digits: 1);

                    ImGui.Unindent();

                    break;

                case CustomComboPreset.SCH_DPS_ChainStrat:
                    DrawHorizontalRadioButton(SCH_ST_DPS_ChainStratagemSubOption,
                        "All content",
                        $"Uses {ActionWatching.GetActionName(ChainStratagem)} regardless of content.", 0);

                    DrawHorizontalRadioButton(SCH_ST_DPS_ChainStratagemSubOption,
                        "Boss encounters Only",
                        $"Only uses {ActionWatching.GetActionName(ChainStratagem)} when in Boss encounters.", 1);

                    DrawSliderInt(0, 100, SCH_ST_DPS_ChainStratagemOption, "Stop using at Enemy HP%. Set to Zero to disable this check.");
                    break;

                case CustomComboPreset.SCH_DPS_EnergyDrain:
                    DrawSliderInt(0, 60, SCH_ST_DPS_EnergyDrain, "Aetherflow remaining cooldown");
                    break;
                
                case CustomComboPreset.SCH_AoE_Lucid:
                    DrawSliderInt(4000, 9500, SCH_AoE_DPS_LucidOption, "MP Threshold", 150, Hundreds);
                    break;
                
                case CustomComboPreset.SCH_AoE_ChainStrat:
                    DrawHorizontalRadioButton(SCH_AoE_DPS_ChainStratagemSubOption,
                        "All content",
                        $"Uses {ActionWatching.GetActionName(ChainStratagem)} regardless of content.", 0);

                    DrawHorizontalRadioButton(SCH_AoE_DPS_ChainStratagemSubOption,
                        "Boss encounters Only",
                        $"Only uses {ActionWatching.GetActionName(ChainStratagem)} when in Boss encounters.", 1);

                    DrawSliderInt(0, 100, SCH_AoE_DPS_ChainStratagemOption, "Stop using at Enemy HP%. Set to Zero to disable this check.");
                    break;

                case CustomComboPreset.SCH_AoE_EnergyDrain:
                    DrawSliderInt(0, 60, SCH_AoE_DPS_EnergyDrain, "Aetherflow remaining cooldown");
                    break;
                #endregion
                
                #region Healing
                case CustomComboPreset.SCH_ST_Heal:
                    DrawAdditionalBoolChoice(SCH_ST_Heal_Adv, "Advanced Options", "", isConditionalChoice: true);
                    if (SCH_ST_Heal_Adv)
                    {
                        ImGui.Indent();
                        DrawAdditionalBoolChoice(SCH_ST_Heal_IncludeShields, "Include Shields in HP Percent Sliders", "");
                        ImGui.Unindent();
                    }
                    break;

                case CustomComboPreset.SCH_ST_Heal_Lucid:
                    DrawSliderInt(4000, 9500, SCH_ST_Heal_LucidOption, "MP Threshold", 150, Hundreds);
                    break;

                case CustomComboPreset.SCH_ST_Heal_Lustrate:
                    DrawSliderInt(0, 100, SCH_ST_Heal_LustrateOption, "Start using when below HP %. Set to 100 to disable this check");
                    DrawPriorityInput(SCH_ST_Heals_Priority, 8, 0, $"{Lustrate.ActionName()} Priority: ");
                    break;

                case CustomComboPreset.SCH_ST_Heal_Excogitation:
                    DrawSliderInt(0, 100, SCH_ST_Heal_ExcogitationOption, "Start using when below HP %. Set to 100 to disable this check");
                    DrawPriorityInput(SCH_ST_Heals_Priority, 8, 1, $"{Excogitation.ActionName()} Priority: ");
                    break;

                case CustomComboPreset.SCH_ST_Heal_Protraction:
                    DrawSliderInt(0, 100, SCH_ST_Heal_ProtractionOption, "Start using when below HP %. Set to 100 to disable this check");
                    DrawPriorityInput(SCH_ST_Heals_Priority, 8, 2, $"{Protraction.ActionName()} Priority: ");
                    break;

                case CustomComboPreset.SCH_ST_Heal_Aetherpact:
                    DrawSliderInt(0, 100, SCH_ST_Heal_AetherpactOption, "Start using when below HP %. Set to 100 to disable this check");
                    DrawSliderInt(0, 100, SCH_ST_Heal_AetherpactDissolveOption, "Stop using when above HP %.");
                    DrawSliderInt(10, 100, SCH_ST_Heal_AetherpactFairyGauge, "Minimal Fairy Gauge to start using Aetherpact", sliderIncrement: Tens);
                    DrawPriorityInput(SCH_ST_Heals_Priority, 8, 3, $"{Aetherpact.ActionName()} Priority: ");
                    break;
                
                case CustomComboPreset.SCH_ST_Heal_WhisperingDawn:
                    DrawSliderInt(0, 100, SCH_ST_Heal_WhisperingDawnOption, "Start using when below HP %. Set to 100 to disable this check");
                    DrawAdditionalBoolChoice(SCH_ST_Heal_WhisperingDawnBossOption, "Not on Bosses", "Will not use on ST in Boss encounters.");
                    DrawPriorityInput(SCH_ST_Heals_Priority, 8, 5, $"{WhisperingDawn.ActionName()} Priority: ");
                    break;

                case CustomComboPreset.SCH_ST_Heal_FeyIllumination:
                    DrawSliderInt(0, 100, SCH_ST_Heal_FeyIlluminationOption, "Start using when below HP %. Set to 100 to disable this check");
                    DrawAdditionalBoolChoice(SCH_ST_Heal_FeyIlluminationBossOption, "Not on Bosses", "Will not use on ST in Boss encounters.");
                    DrawPriorityInput(SCH_ST_Heals_Priority, 8, 6, $"{FeyIllumination.ActionName()} Priority: ");
                    break;

                case CustomComboPreset.SCH_ST_Heal_FeyBlessing:
                    DrawSliderInt(0, 100, SCH_ST_Heal_FeyBlessingOption, "Start using when below HP %. Set to 100 to disable this check");
                    DrawAdditionalBoolChoice(SCH_ST_Heal_FeyBlessingBossOption, "Not on Bosses", "Will not use on ST in Boss encounters.");
                    DrawPriorityInput(SCH_ST_Heals_Priority, 8, 7, $"{FeyBlessing.ActionName()} Priority: ");
                    break;
                
                case CustomComboPreset.SCH_ST_Heal_Adloquium:
                    DrawSliderInt(0, 100, SCH_ST_Heal_AdloquiumOption,"Start using when below HP %. Set to 100 to disable this check.");
                    DrawHorizontalMultiChoice(SCH_ST_Heal_AldoquimOpts,"Scholar Shield Check", "Enable to not override an existing Scholar's shield.", 3, 0);
                    DrawHorizontalMultiChoice(SCH_ST_Heal_AldoquimOpts,"Sage Shield Check", "Enable to not override an existing Sage's shield.", 3, 1);
                    DrawHorizontalMultiChoice(SCH_ST_Heal_AldoquimOpts,"Emergency Tactics","Will use Emergency tactics before Adloquim when below set threshold", 3, 2);
                    
                    if (SCH_ST_Heal_AldoquimOpts[2])
                    {
                        ImGui.Indent();
                        DrawSliderInt(0, 100, SCH_ST_Heal_AdloquiumOption_Emergency,"Start using when below HP %. Set to 100 to disable this check.");
                        ImGui.Unindent();
                    }
                    
                    DrawPriorityInput(SCH_ST_Heals_Priority, 8, 4, $"{Adloquium.ActionName()} Priority: ");
                    break;

                case CustomComboPreset.SCH_ST_Heal_Esuna:
                    DrawSliderInt(0, 100, SCH_ST_Heal_EsunaOption, "Stop using when below HP %. Set to Zero to disable this check");
                    break;
                
                case CustomComboPreset.SCH_AoE_Heal_Lucid:
                    DrawSliderInt(4000, 9500, SCH_AoE_Heal_LucidOption, "MP Threshold", 150, Hundreds);
                    break;

                case CustomComboPreset.SCH_AoE_Heal:
                    ImGui.TextUnformatted("Note: Succor will always be available.");
                    ImGui.TextUnformatted("These options are to provide optional priority to Succor or to set up Emergency tactics option.");
                    DrawSliderInt(0, 100, SCH_AoE_Heal_SuccorShieldOption, "Shield Check: Will use when less than set percentage of party have shields.", sliderIncrement: 25);
                    DrawPriorityInput(SCH_AoE_Heals_Priority, 8, 7, $"{Succor.ActionName()} Priority: ");
                    DrawHorizontalMultiChoice(SCH_AoE_Heal_Succor_Options,"Emergency Tactics","If more than the set percentage of the party has shields, will use Emergency Tactics before Succor", 2, 0);
                    DrawHorizontalMultiChoice(SCH_AoE_Heal_Succor_Options,"Recitation","Will use Recitation to buff Succor", 2, 1);
                    break;

                case CustomComboPreset.SCH_AoE_Heal_WhisperingDawn:
                    DrawSliderInt(0, 100, SCH_AoE_Heal_WhisperingDawnOption, "Start using when below party average HP %. Set to 100 to disable this check");
                    DrawPriorityInput(SCH_AoE_Heals_Priority, 8, 0, $"{WhisperingDawn.ActionName()} Priority: ");
                    break;

                case CustomComboPreset.SCH_AoE_Heal_FeyIllumination:
                    DrawSliderInt(0, 100, SCH_AoE_Heal_FeyIlluminationOption, "Start using when below party average HP %. Set to 100 to disable this check");
                    DrawPriorityInput(SCH_AoE_Heals_Priority, 8, 1, $"{FeyIllumination.ActionName()} Priority: ");
                    break;

                case CustomComboPreset.SCH_AoE_Heal_FeyBlessing:
                    DrawSliderInt(0, 100, SCH_AoE_Heal_FeyBlessingOption, "Start using when below party average HP %. Set to 100 to disable this check");
                    DrawPriorityInput(SCH_AoE_Heals_Priority, 8, 2, $"{FeyBlessing.ActionName()} Priority: ");
                    break;

                case CustomComboPreset.SCH_AoE_Heal_Consolation:
                    DrawSliderInt(0, 100, SCH_AoE_Heal_ConsolationOption, "Start using when below party average HP %. Set to 100 to disable this check");
                    DrawPriorityInput(SCH_AoE_Heals_Priority, 8, 3, $"{Consolation.ActionName()} Priority: ");
                    break;
                
                case CustomComboPreset.SCH_AoE_Heal_SummonSeraph:
                    DrawSliderInt(0, 100, SCH_AoE_Heal_SummonSeraph, "Start using when below party average HP %. Set to 100 to disable this check");
                    DrawPriorityInput(SCH_AoE_Heals_Priority, 8, 6, $"{SummonSeraph.ActionName()} Priority: ");
                    break;

                case CustomComboPreset.SCH_AoE_Heal_Seraphism:
                    DrawSliderInt(0, 100, SCH_AoE_Heal_SeraphismOption, "Start using when below party average HP %. Set to 100 to disable this check");
                    DrawPriorityInput(SCH_AoE_Heals_Priority, 8, 4, $"{Seraphism.ActionName()} Priority: ");
                    break;

                case CustomComboPreset.SCH_AoE_Heal_Indomitability:
                    DrawSliderInt(0, 100, SCH_AoE_Heal_IndomitabilityOption, "Start using when below party average HP %. Set to 100 to disable this check");
                    DrawAdditionalBoolChoice(SCH_AoE_Heal_Indomitability_Recitation, "Recitation Option", "Will use Recitation to buff Indomitability.");
                    DrawPriorityInput(SCH_AoE_Heals_Priority, 8, 5, $"{Indomitability.ActionName()} Priority: ");
                    break;
                
                #endregion
                
                #region Standalones
                case CustomComboPreset.SCH_Aetherflow:
                    DrawRadioButton(SCH_Aetherflow_Display, "Show Aetherflow On Energy Drain Only", "", 0);
                    DrawRadioButton(SCH_Aetherflow_Display, "Show Aetherflow On All Aetherflow Skills", "", 1);
                    break;

                case CustomComboPreset.SCH_Aetherflow_Recite:
                    DrawAdditionalBoolChoice(SCH_Aetherflow_Recite_Excog, "On Excogitation", "", isConditionalChoice: true);
                    if (SCH_Aetherflow_Recite_Excog)
                    {
                        ImGui.Indent();
                        ImGui.Spacing();
                        DrawRadioButton(SCH_Aetherflow_Recite_ExcogMode, "Only when out of Aetherflow Stacks", "", 0);
                        DrawRadioButton(SCH_Aetherflow_Recite_ExcogMode, "Always when available", "", 1);
                        ImGui.Unindent();
                    }

                    DrawAdditionalBoolChoice(SCH_Aetherflow_Recite_Indom, "On Indominability", "", isConditionalChoice: true);
                    if (SCH_Aetherflow_Recite_Indom)
                    {
                        ImGui.Indent();
                        ImGui.Spacing();
                        DrawRadioButton(SCH_Aetherflow_Recite_IndomMode, "Only when out of Aetherflow Stacks", "", 0);
                        DrawRadioButton(SCH_Aetherflow_Recite_IndomMode, "Always when available", "", 1);
                        ImGui.Unindent();
                    }
                    break;

                case CustomComboPreset.SCH_Recitation:
                    DrawRadioButton(SCH_Recitation_Mode, "Adloquium", "", 0);
                    DrawRadioButton(SCH_Recitation_Mode, "Succor", "", 1);
                    DrawRadioButton(SCH_Recitation_Mode, "Indomitability", "", 2);
                    DrawRadioButton(SCH_Recitation_Mode, "Excogitation", "", 3);
                    break; 
                #endregion
            }
        }
        
    #region Options
    
        #region DPS

        public static UserInt
            SCH_ST_DPS_LucidOption = new("SCH_ST_DPS_LucidOption", 6500),
            SCH_AoE_DPS_LucidOption = new("SCH_AoE_LucidOption", 6500),
            SCH_ST_DPS_OpenerOption = new("SCH_ST_DPS_OpenerOption"),
            SCH_ST_DPS_OpenerContent = new("SCH_ST_DPS_OpenerContent", 1),
            SCH_ST_DPS_ChainStratagemOption = new("SCH_ST_DPS_ChainStratagemOption", 10),
            SCH_AoE_DPS_ChainStratagemOption = new("SCH_AoE_DPS_ChainStratagemOption", 10),
            SCH_DPS_BioOption = new("SCH_DPS_BioOption"),
            SCH_DPS_BioSubOption = new("SCH_DPS_BioSubOption", 0),
            SCH_ST_DPS_EnergyDrain = new("SCH_ST_DPS_EnergyDrain", 3),
            SCH_ST_DPS_ChainStratagemSubOption = new("SCH_ST_DPS_ChainStratagemSubOption", 1),
            SCH_AoE_DPS_EnergyDrain = new("SCH_AoE_DPS_EnergyDrain", 3),
            SCH_AoE_DPS_ChainStratagemSubOption = new("SCH_AoE_DPS_ChainStratagemSubOption", 1);
        public static UserBool
            SCH_ST_DPS_Adv = new("SCH_ST_DPS_Adv");

        public static UserFloat
            SCH_DPS_BioUptime_Threshold = new("SCH_DPS_BioUptime_Threshold", 3.0f);
            
        public static UserBoolArray
            SCH_ST_DPS_Adv_Actions = new("SCH_ST_DPS_Adv_Actions");

        #endregion

        #region Healing

        public static UserInt
            
            SCH_AoE_Heal_LucidOption = new("SCH_AoE_Heal_LucidOption", 8000),
            SCH_AoE_Heal_SuccorShieldOption = new("SCH_AoE_Heal_SuccorShieldCount", 50),
            SCH_AoE_Heal_WhisperingDawnOption = new("SCH_AoE_Heal_WhisperingDawnOption", 70),
            SCH_AoE_Heal_FeyIlluminationOption = new("SCH_AoE_Heal_FeyIlluminationOption", 50),
            SCH_AoE_Heal_ConsolationOption = new("SCH_AoE_Heal_ConsolationOption", 60),
            SCH_AoE_Heal_FeyBlessingOption = new("SCH_AoE_Heal_FeyBlessingOption", 60),
            SCH_AoE_Heal_SeraphismOption = new("SCH_AoE_Heal_SeraphismOption", 30),
            SCH_AoE_Heal_IndomitabilityOption = new("SCH_AoE_Heal_IndomitabilityOption", 70),
            SCH_AoE_Heal_SummonSeraph = new("SCH_AoE_Heal_SummonSeraph", 40),
            SCH_ST_Heal_LucidOption = new("SCH_ST_Heal_LucidOption", 8000),
            SCH_ST_Heal_AdloquiumOption = new("SCH_ST_Heal_AdloquiumOption", 70),
            SCH_ST_Heal_AdloquiumOption_Emergency= new("SCH_ST_Heal_AdloquiumOption_Emergency", 30),
            SCH_ST_Heal_LustrateOption = new("SCH_ST_Heal_LustrateOption", 70),
            SCH_ST_Heal_ExcogitationOption = new("SCH_ST_Heal_ExcogitationOption", 50),
            SCH_ST_Heal_ProtractionOption = new("SCH_ST_Heal_ProtractionOption", 30),
            SCH_ST_Heal_AetherpactOption = new("SCH_ST_Heal_AetherpactOption", 60),
            SCH_ST_Heal_AetherpactDissolveOption = new("SCH_ST_Heal_AetherpactDissolveOption", 90),
            SCH_ST_Heal_AetherpactFairyGauge = new("SCH_ST_Heal_AetherpactFairyGauge", 50),
            SCH_ST_Heal_WhisperingDawnOption = new("SCH_ST_Heal_WhisperingDawnOption", 70),
            SCH_ST_Heal_FeyIlluminationOption = new("SCH_ST_Heal_FeyIlluminationOption", 70),
            SCH_ST_Heal_FeyBlessingOption = new("SCH_ST_Heal_FeyBlessingOption", 70),
            SCH_ST_Heal_EsunaOption = new("SCH_ST_Heal_EsunaOption", 30);
        public static UserIntArray
            SCH_ST_Heals_Priority = new("SCH_ST_Heals_Priority"),
            SCH_AoE_Heals_Priority = new("SCH_AoE_Heals_Priority");

        public static UserBool
            SCH_ST_Heal_Adv = new("SCH_ST_Heal_Adv"),
            SCH_ST_Heal_IncludeShields = new("SCH_ST_Heal_IncludeShields"),
            SCH_ST_Heal_WhisperingDawnBossOption = new("SCH_ST_Heal_WhisperingDawnBossOption"),
            SCH_ST_Heal_FeyIlluminationBossOption = new("SCH_ST_Heal_FeyIlluminationBossOption"),
            SCH_ST_Heal_FeyBlessingBossOption = new("SCH_ST_Heal_FeyBlessingBossOption"),
            SCH_AoE_Heal_Indomitability_Recitation = new("SCH_AoE_Heal_Indomitability_Recitation");

        public static UserBoolArray
            SCH_ST_Heal_AldoquimOpts = new("SCH_ST_Heal_AldoquimOpts"),
            SCH_AoE_Heal_Succor_Options = new("SCH_AoE_Heal_Succor_Options");

        #endregion

        #region Standalones

        internal static UserBool
            SCH_Aetherflow_Recite_Indom = new("SCH_Aetherflow_Recite_Indom"),
            SCH_Aetherflow_Recite_Excog = new("SCH_Aetherflow_Recite_Excog");
        internal static UserInt
            SCH_Aetherflow_Display = new("SCH_Aetherflow_Display"),
            SCH_Aetherflow_Recite_ExcogMode = new("SCH_Aetherflow_Recite_ExcogMode"),
            SCH_Aetherflow_Recite_IndomMode = new("SCH_Aetherflow_Recite_IndomMode"),
            SCH_Recitation_Mode = new("SCH_Recitation_Mode");

        #endregion
        
    #endregion
    
    }
}
