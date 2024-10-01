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
    public static NoRandomIdeologiesMod instance;

    private static string currentVersion;

    private static Vector2 scrollPosition;

    private static readonly Color alternateBackground = new Color(0.2f, 0.2f, 0.2f, 0.5f);

    public static readonly Dictionary<FactionDef, Tuple<List<Ideo>, string>> FactionSelectionCache = [];

    /// <summary>
    ///     Constructor
    /// </summary>
    /// <param name="content"></param>
    public NoRandomIdeologiesMod(ModContentPack content) : base(content)
    {
        instance = this;
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
        var listing_Standard = new Listing_Standard();
        listing_Standard.Begin(rect);
        if (currentVersion != null)
        {
            GUI.contentColor = Color.gray;
            listing_Standard.Label("NRI.modVersion".Translate(currentVersion));
            GUI.contentColor = Color.white;
        }

        Settings.PercentChance = listing_Standard.SliderLabeled(
            "NRI.percentChance".Translate(Settings.PercentChance.ToStringPercent()),
            Settings.PercentChance, 0, 1f, tooltip: "NRI.percentChanceTT".Translate());

        if (Settings.PreferedIdeology.Any() &&
            listing_Standard.ButtonTextLabeledPct("NRI.resetAllFactions".Translate(), "NRI.reset".Translate(), 0.7f))
        {
            listing_Standard.Gap();
            Settings.PreferedIdeology.Clear();
        }

        listing_Standard.End();

        var outerRect = rect;
        outerRect.y += listing_Standard.CurHeight + 30f;
        outerRect.height -= listing_Standard.CurHeight + 30f;
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

            var savedValueFound =
                instance.Settings.PreferedIdeology.TryGetValue(factionDef.defName, out var selectedIdeology);
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
                    default:
                        buttonText = selectedIdeology;
                        break;
                }
            }
            else
            {
                buttonText = "NRI.useRandomSaved".Translate().RawText;
            }

            if (!Widgets.ButtonText(row.RightPart(0.3f).ExpandedBy(0, 4f), buttonText))
            {
                continue;
            }

            var options = new List<FloatMenuOption>
            {
                new FloatMenuOption("NRI.useRandomSaved".Translate(),
                    delegate { instance.Settings.PreferedIdeology.Remove(factionDef.defName); },
                    mouseoverGuiAction: tooltipRect =>
                        TooltipHandler.TipRegion(tooltipRect, "NRI.useRandomSavedTT".Translate())),
                new FloatMenuOption("NRI.useRandomOrSaved".Translate(),
                    delegate
                    {
                        instance.Settings.PreferedIdeology[factionDef.defName] = NoRandomIdeologies.PercentSaveString;
                    },
                    mouseoverGuiAction: tooltipRect =>
                        TooltipHandler.TipRegion(tooltipRect, "NRI.useRandomOrSavedTT".Translate())),
                new FloatMenuOption("NRI.useGenerated".Translate(),
                    delegate
                    {
                        instance.Settings.PreferedIdeology[factionDef.defName] = NoRandomIdeologies.VanillaSaveString;
                    },
                    mouseoverGuiAction: tooltipRect =>
                        TooltipHandler.TipRegion(tooltipRect, "NRI.useGeneratedTT".Translate()))
            };
            foreach (var ideo in validIdeologies.OrderBy(ideo => ideo.name))
            {
                options.Add(new FloatMenuOption(ideo.name,
                    delegate { instance.Settings.PreferedIdeology[factionDef.defName] = ideo.name; }, ideo.Icon,
                    ideo.primaryFactionColor ?? Color.white,
                    mouseoverGuiAction: tooltipRect => TooltipHandler.TipRegion(tooltipRect, ideo.description)));
            }

            Find.WindowStack.Add(new FloatMenu(options));
        }

        listing_ScrollView.End();
        Widgets.EndScrollView();
    }

    public override void WriteSettings()
    {
        base.WriteSettings();
        NoRandomIdeologies.LastCheck = DateTime.MinValue;
    }
}