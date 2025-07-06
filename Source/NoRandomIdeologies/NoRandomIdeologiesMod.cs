using System;
using System.Collections.Generic;
using System.Linq;
using Mlie;
using RimWorld;
using UnityEngine;
using Verse;

namespace NoRandomIdeologies;

[StaticConstructorOnStartup]
internal class NoRandomIdeologiesMod : Mod
{
    /// <summary>
    ///     The instance of the settings to be read by the mod
    /// </summary>
    public static NoRandomIdeologiesMod Instance;

    private static string currentVersion;

    private static Vector2 scrollPosition;

    private static readonly Color alternateBackground = new(0.2f, 0.2f, 0.2f, 0.5f);

    public static readonly Dictionary<FactionDef, Tuple<List<Ideo>, string>> FactionSelectionCache = [];

    private static string changeAll = string.Empty;

    /// <summary>
    ///     Constructor
    /// </summary>
    /// <param name="content"></param>
    public NoRandomIdeologiesMod(ModContentPack content) : base(content)
    {
        Instance = this;
        Settings = GetSettings<NoRandomIdeologiesSettings>();
        currentVersion = VersionFromManifest.GetVersionFromModMetaData(content.ModMetaData);
    }

    /// <summary>
    ///     The instance-settings for the mod
    /// </summary>
    internal NoRandomIdeologiesSettings Settings { get; }

    /// <summary>
    ///     The title for the mod-settings
    /// </summary>
    /// <returns></returns>
    public override string SettingsCategory()
    {
        return "No Random Ideologies";
    }

    /// <summary>
    ///     The settings-window
    ///     For more info: https://rimworldwiki.com/wiki/Modding_Tutorials/ModSettings
    /// </summary>
    /// <param name="rect"></param>
    public override void DoSettingsWindowContents(Rect rect)
    {
        NoRandomIdeologies.LoadIdeos();
        var listingStandard = new Listing_Standard();
        listingStandard.Begin(rect);

        Settings.PercentChance = listingStandard.SliderLabeled(
            "NRI.percentChance".Translate(Settings.PercentChance.ToStringPercent()),
            Settings.PercentChance, 0, 1f, tooltip: "NRI.percentChanceTT".Translate());
        if (listingStandard.ButtonTextLabeledPct("NRI.changeAll".Translate(), "NRI.changeAllSelect".Translate(), 0.7f))
        {
            var options = new List<FloatMenuOption>
            {
                new("NRI.useRandomSaved".Translate(),
                    delegate { changeAll = NoRandomIdeologies.RandomSavedString; },
                    mouseoverGuiAction: tooltipRect =>
                        TooltipHandler.TipRegion(tooltipRect, "NRI.useRandomSavedTT".Translate())),
                new("NRI.useRandomOrSaved".Translate(),
                    delegate { changeAll = NoRandomIdeologies.PercentSaveString; },
                    mouseoverGuiAction: tooltipRect =>
                        TooltipHandler.TipRegion(tooltipRect, "NRI.useRandomOrSavedTT".Translate())),
                new("NRI.useGenerated".Translate(),
                    delegate { changeAll = NoRandomIdeologies.VanillaSaveString; },
                    mouseoverGuiAction: tooltipRect =>
                        TooltipHandler.TipRegion(tooltipRect, "NRI.useGeneratedTT".Translate()))
            };
            foreach (var ideo in NoRandomIdeologies.SavedIdeos.OrderBy(ideo => ideo.name))
            {
                options.Add(new FloatMenuOption(ideo.name,
                    delegate { changeAll = ideo.name; }, ideo.Icon,
                    ideo.primaryFactionColor ?? Color.white,
                    mouseoverGuiAction: tooltipRect => TooltipHandler.TipRegion(tooltipRect, ideo.description)));
            }

            Find.WindowStack.Add(new FloatMenu(options));
        }

        var defaultButtonText = "NRI.useRandomSaved".Translate();
        switch (Settings.DefaultSetting)
        {
            case NoRandomIdeologies.PercentSaveString:
                defaultButtonText = "NRI.useRandomOrSaved".Translate();
                break;
            case NoRandomIdeologies.VanillaSaveString:
                defaultButtonText = "NRI.useGenerated".Translate();
                break;
        }

        if (listingStandard.ButtonTextLabeledPct("NRI.defaultOption".Translate(), defaultButtonText, 0.7f,
                tooltip: "NRI.defaultOptionTT".Translate()))
        {
            var options = new List<FloatMenuOption>
            {
                new("NRI.useRandomSaved".Translate(),
                    delegate { Settings.DefaultSetting = NoRandomIdeologies.RandomSavedString; },
                    mouseoverGuiAction: tooltipRect =>
                        TooltipHandler.TipRegion(tooltipRect, "NRI.useRandomSavedTT".Translate())),
                new("NRI.useRandomOrSaved".Translate(),
                    delegate { Settings.DefaultSetting = NoRandomIdeologies.PercentSaveString; },
                    mouseoverGuiAction: tooltipRect =>
                        TooltipHandler.TipRegion(tooltipRect, "NRI.useRandomOrSavedTT".Translate())),
                new("NRI.useGenerated".Translate(),
                    delegate { Settings.DefaultSetting = NoRandomIdeologies.VanillaSaveString; },
                    mouseoverGuiAction: tooltipRect =>
                        TooltipHandler.TipRegion(tooltipRect, "NRI.useGeneratedTT".Translate()))
            };
            Find.WindowStack.Add(new FloatMenu(options));
        }

        if (Settings.CanReset() &&
            listingStandard.ButtonTextLabeledPct("NRI.resetAllFactions".Translate(), "NRI.reset".Translate(), 0.7f))
        {
            Settings.Reset();
            FactionSelectionCache.Clear();
        }
        else
        {
            listingStandard.Gap();
            listingStandard.Gap();
        }

        if (currentVersion != null)
        {
            GUI.contentColor = Color.gray;
            listingStandard.Label("NRI.modVersion".Translate(currentVersion));
            GUI.contentColor = Color.white;
        }

        listingStandard.End();

        var outerRect = rect;
        outerRect.y += listingStandard.CurHeight + 30f;
        outerRect.height -= listingStandard.CurHeight + 30f;
        var innerRect = outerRect;
        innerRect.width -= 20;
        innerRect.x = 0;
        innerRect.y = 0;
        innerRect.height = 36f * NoRandomIdeologies.AllFactionDefs.Count;
        Widgets.BeginScrollView(outerRect, ref scrollPosition, innerRect);
        var listing_ScrollView = new Listing_Standard();
        listing_ScrollView.Begin(innerRect);
        var alternate = false;
        foreach (var factionDef in NoRandomIdeologies.AllFactionDefs)
        {
            alternate = !alternate;
            var outerRow = listing_ScrollView.GetRect(36f);
            if (alternate)
            {
                Widgets.DrawBoxSolid(outerRow, alternateBackground);
            }

            var row = outerRow.ContractedBy(2f, 6f);
            var originalColor = GUI.color;

            Widgets.Label(row.LeftPart(0.65f).RightPart(0.95f).TopPartPixels(25f).CenteredOnYIn(row),
                factionDef.LabelCap);
            if (factionDef.FactionIcon != BaseContent.BadTex)
            {
                GUI.color = factionDef.DefaultColor;
                Widgets.DrawTextureFitted(row.LeftPart(0.65f).LeftPart(0.05f), factionDef.FactionIcon, 1);
                GUI.color = originalColor;
            }

            TooltipHandler.TipRegion(row.LeftPart(0.65f), factionDef.description);

            var ignore = Settings.FactionIgnore.Contains(factionDef.defName);
            var ignoreWas = ignore;
            Widgets.CheckboxLabeled(row.RightPart(0.6f).LeftHalf(), "NRI.overrideRestrictions".Translate(), ref ignore);
            TooltipHandler.TipRegion(row.RightPart(0.6f).LeftHalf(), "NRI.overrideRestrictionsTT".Translate());
            if (ignore != ignoreWas)
            {
                FactionSelectionCache.Remove(factionDef);
                if (ignore)
                {
                    Settings.FactionIgnore.Add(factionDef.defName);
                    factionDef.fixedIdeo = false;
                    factionDef.requiredMemes = [];
                }
                else
                {
                    Settings.FactionIgnore.Remove(factionDef.defName);
                    factionDef.fixedIdeo = NoRandomIdeologies.VanillaFixedIdeologies[factionDef];
                    factionDef.requiredMemes = NoRandomIdeologies.VanillaRequiredMemes[factionDef];
                }
            }

            List<Ideo> validIdeologies;
            string toolTip;
            if (FactionSelectionCache.TryGetValue(factionDef, out var ideoInfo))
            {
                validIdeologies = ideoInfo.Item1;
                toolTip = ideoInfo.Item2;
            }
            else
            {
                validIdeologies = NoRandomIdeologies.FindAllValidIdeologies(factionDef, out toolTip);
                FactionSelectionCache[factionDef] = Tuple.Create(validIdeologies, toolTip);
            }


            if (validIdeologies.Count == 0)
            {
                Widgets.ButtonText(row.RightPart(0.3f).ExpandedBy(0, 4f), "NRI.noneFound".Translate(), false, false,
                    false);
                if (!string.IsNullOrEmpty(toolTip))
                {
                    TooltipHandler.TipRegion(row.RightPart(0.3f), toolTip);
                }

                continue;
            }

            if (!string.IsNullOrEmpty(changeAll))
            {
                switch (changeAll)
                {
                    case NoRandomIdeologies.RandomSavedString:
                    case NoRandomIdeologies.PercentSaveString:
                    case NoRandomIdeologies.VanillaSaveString:
                        Settings.PreferredIdeology[factionDef.defName] = changeAll;
                        break;
                    default:
                        if (validIdeologies.Any(ideo => ideo.name == changeAll))
                        {
                            Settings.PreferredIdeology[factionDef.defName] = changeAll;
                        }

                        break;
                }

                continue;
            }

            var savedValueFound =
                Instance.Settings.PreferredIdeology.TryGetValue(factionDef.defName, out var selectedIdeology);
            string buttonText;
            if (savedValueFound)
            {
                switch (selectedIdeology)
                {
                    case NoRandomIdeologies.VanillaSaveString:
                        buttonText = "NRI.useGenerated".Translate().RawText;
                        break;
                    case NoRandomIdeologies.PercentSaveString:
                        buttonText = "NRI.useRandomOrSaved".Translate().RawText;
                        break;
                    case NoRandomIdeologies.RandomSavedString:
                        buttonText = "NRI.useRandomSaved".Translate().RawText;
                        break;
                    default:
                        var selectedSplitted = selectedIdeology.Split(NoRandomIdeologies.SaveStringSplitter);
                        if (selectedSplitted.Length > 1)
                        {
                            buttonText = "NRI.multipleSelected".Translate(selectedSplitted.Length);
                            break;
                        }

                        buttonText = selectedIdeology;
                        break;
                }
            }
            else
            {
                buttonText = "NRI.useRandomSaved".Translate().RawText;
                Instance.Settings.PreferredIdeology[factionDef.defName] = NoRandomIdeologies.RandomSavedString;
            }

            if (!Widgets.ButtonText(row.RightPart(0.3f).ExpandedBy(0, 4f), buttonText))
            {
                continue;
            }

            var options = new List<FloatMenuOption>
            {
                new("NRI.useRandomSaved".Translate(),
                    delegate
                    {
                        Instance.Settings.PreferredIdeology[factionDef.defName] = NoRandomIdeologies.RandomSavedString;
                    },
                    mouseoverGuiAction: tooltipRect =>
                        TooltipHandler.TipRegion(tooltipRect, "NRI.useRandomSavedTT".Translate())),
                new("NRI.useRandomOrSaved".Translate(),
                    delegate
                    {
                        Instance.Settings.PreferredIdeology[factionDef.defName] = NoRandomIdeologies.PercentSaveString;
                    },
                    mouseoverGuiAction: tooltipRect =>
                        TooltipHandler.TipRegion(tooltipRect, "NRI.useRandomOrSavedTT".Translate())),
                new("NRI.useGenerated".Translate(),
                    delegate
                    {
                        Instance.Settings.PreferredIdeology[factionDef.defName] = NoRandomIdeologies.VanillaSaveString;
                    },
                    mouseoverGuiAction: tooltipRect =>
                        TooltipHandler.TipRegion(tooltipRect, "NRI.useGeneratedTT".Translate()))
            };
            foreach (var ideo in validIdeologies.OrderBy(ideo => ideo.name))
            {
                var menuItem = new FloatMenuOption(ideo.name,
                    factionManagement, ideo.Icon, ideo.primaryFactionColor ?? Color.white,
                    mouseoverGuiAction: tooltipRect => TooltipHandler.TipRegion(tooltipRect, ideo.description))
                {
                    extraPartRightJustified = true,
                    extraPartWidth = 29f,
                    extraPartOnGUI = extraPartOnGui
                };

                options.Add(menuItem);
                continue;

                void factionManagement()
                {
                    if (!Instance.Settings.PreferredIdeology.TryGetValue(factionDef.defName, out var ideologyValue))
                    {
                        Instance.Settings.PreferredIdeology[factionDef.defName] = ideo.name;
                        return;
                    }

                    switch (ideologyValue)
                    {
                        case NoRandomIdeologies.VanillaSaveString:
                        case NoRandomIdeologies.PercentSaveString:
                        case NoRandomIdeologies.RandomSavedString:
                            Instance.Settings.PreferredIdeology[factionDef.defName] = ideo.name;
                            return;
                    }

                    var selectedIdeologies = ideologyValue.Split(NoRandomIdeologies.SaveStringSplitter).ToList();

                    if (selectedIdeologies.Contains(ideo.name))
                    {
                        selectedIdeologies.RemoveWhere(name => name == ideo.name);
                    }
                    else
                    {
                        selectedIdeologies.Add(ideo.name);
                    }

                    if (selectedIdeologies.Count == 0)
                    {
                        Instance.Settings.PreferredIdeology.Remove(factionDef.defName);
                        return;
                    }

                    Instance.Settings.PreferredIdeology[factionDef.defName] =
                        string.Join(NoRandomIdeologies.SaveStringSplitter.ToString(), selectedIdeologies);
                }

                bool extraPartOnGui(Rect iconRect)
                {
                    var newRect = new Rect(iconRect.x + 5f, iconRect.y + ((iconRect.height - 24f) / 2f), 24f, 24f);
                    if (Widgets.ButtonInvisible(newRect))
                    {
                        factionManagement();
                    }

                    var selected = Instance.Settings.PreferredIdeology[factionDef.defName]
                        .Split(NoRandomIdeologies.SaveStringSplitter).Any(name => name == ideo.name);
                    Widgets.DrawTextureFitted(newRect, selected ? Widgets.CheckboxOnTex : Widgets.CheckboxOffTex, 1f);
                    return false;
                }
            }

            Find.WindowStack.Add(new FloatMenu(options));
        }

        listing_ScrollView.End();
        Widgets.EndScrollView();
        changeAll = string.Empty;
    }

    public override void WriteSettings()
    {
        base.WriteSettings();
        NoRandomIdeologies.LastCheck = DateTime.MinValue;
    }
}