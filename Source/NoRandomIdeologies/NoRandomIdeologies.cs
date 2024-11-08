using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace NoRandomIdeologies;

[StaticConstructorOnStartup]
public static class NoRandomIdeologies
{
    public const string VanillaSaveString = "./Vanilla";
    public const string RandomSavedString = "./RandomSaved";
    public const string PercentSaveString = "./UsePercent";
    public const char SaveStringSplitter = '|';
    public static DateTime LastCheck;
    public static readonly List<Ideo> SavedIdeos = [];

    public static readonly Dictionary<FactionDef, bool> VanillaFixedIdologies;
    public static readonly Dictionary<FactionDef, List<MemeDef>> VanillaRequiredMemes;


    private static readonly FieldInfo isSavingOrLoadingExternalIdeoFieldInfo =
        AccessTools.Field(typeof(GameDataSaveLoader), "isSavingOrLoadingExternalIdeo");

    private static readonly FieldInfo reachedMaxMessagesLimitFieldInfo =
        AccessTools.Field(typeof(Log), "reachedMaxMessagesLimit");

    public static readonly List<FactionDef> AllFactionDefs;

    static NoRandomIdeologies()
    {
        AllFactionDefs = DefDatabase<FactionDef>.AllDefsListForReading.Where(def => !def.isPlayer)
            .OrderBy(def => def.label).ToList();
        VanillaFixedIdologies = [];
        VanillaRequiredMemes = [];
        if (NoRandomIdeologiesMod.instance.Settings.FactionIgnore == null)
        {
            NoRandomIdeologiesMod.instance.Settings.FactionIgnore = [];
        }

        foreach (var factionDef in AllFactionDefs)
        {
            VanillaFixedIdologies[factionDef] = factionDef.fixedIdeo;
            VanillaRequiredMemes[factionDef] = factionDef.requiredMemes;
            if (!NoRandomIdeologiesMod.instance.Settings.FactionIgnore.Contains(factionDef.defName))
            {
                continue;
            }

            factionDef.fixedIdeo = false;
            factionDef.requiredMemes = [];
        }

        new Harmony("Mlie.NoRandomIdeologies").PatchAll(Assembly.GetExecutingAssembly());
    }

    public static void LoadIdeos()
    {
        if (!GenFilePaths.AllCustomIdeoFiles.Any())
        {
            Log.WarningOnce("[NoRandomIdeologies]: No saved ideologies found.",
                "No saved ideologies found".GetHashCode());
            return;
        }

        var lastWriteTime = GenFilePaths.AllCustomIdeoFiles.OrderByDescending(info => info.LastWriteTime).First()
            .LastWriteTime;
        if (LastCheck == lastWriteTime)
        {
            return;
        }

        NoRandomIdeologiesMod.FactionSelectionCache.Clear();
        LastCheck = lastWriteTime;

        var currentIdeoFiles = new List<SaveFileInfo>();
        foreach (var allCustomIdeoFile in GenFilePaths.AllCustomIdeoFiles)
        {
            try
            {
                var saveFileInfo = new SaveFileInfo(allCustomIdeoFile);
                saveFileInfo.LoadData();
                currentIdeoFiles.Add(saveFileInfo);
            }
            catch (Exception)
            {
                // Ignore
            }
        }

        if (!currentIdeoFiles.Any())
        {
            Log.WarningOnce("[NoRandomIdeologies]: No valid saved ideologies found.",
                "No valid saved ideologies found".GetHashCode());
            return;
        }

        SavedIdeos.Clear();
        Log.Message("[NoRandomIdeologies]: Loading saved ideologies into cache. " +
                    "This may cause warnings if any saved ideology have precepts or memes that uses content from not loaded mods. " +
                    "These will just be skipped and the warnings can be ignored.");
        foreach (var file in currentIdeoFiles)
        {
            var filePath = GenFilePaths.AbsPathForIdeo(Path.GetFileNameWithoutExtension(file.FileName));
            Ideo currentIdeo = null;
            try
            {
                Scribe.ForceStop();
                isSavingOrLoadingExternalIdeoFieldInfo.SetValue(null, true);
                Scribe.loader.InitLoading(filePath);
                ScribeMetaHeaderUtility.LoadGameDataHeader(ScribeMetaHeaderUtility.ScribeHeaderMode.Ideo, false);
                var currentLogValue = (bool)reachedMaxMessagesLimitFieldInfo.GetValue(null);
                if (!currentLogValue)
                {
                    reachedMaxMessagesLimitFieldInfo.SetValue(null, true);
                }

                Scribe_Deep.Look(ref currentIdeo, "ideo");
                if (!currentLogValue)
                {
                    reachedMaxMessagesLimitFieldInfo.SetValue(null, false);
                }
            }
            catch
            {
                Log.WarningOnce($"[NoRandomIdeologies]: Saved ideology {filePath} skipped since it couldnt be loaded.",
                    filePath.GetHashCode());
                continue;
            }

            if (currentIdeo == null)
            {
                Log.WarningOnce($"[NoRandomIdeologies]: Saved ideology {filePath} skipped since it couldnt be loaded.",
                    filePath.GetHashCode());
                continue;
            }

            if (!currentIdeo.PreceptsListForReading.Any() || currentIdeo.PreceptsListForReading.Any(precept =>
                    precept == null))
            {
                Log.WarningOnce(
                    $"[NoRandomIdeologies]: Saved ideology {currentIdeo} skipped since it has a precepts that is not loaded.",
                    filePath.GetHashCode());
                continue;
            }

            if (!currentIdeo.memes.Any() || currentIdeo.memes.Any(memeDef => memeDef == null))
            {
                Log.WarningOnce(
                    $"[NoRandomIdeologies]: Saved ideology {currentIdeo} skipped since it has memes that are not loaded.",
                    filePath.GetHashCode());
                continue;
            }

            if (currentIdeo.VeneratedAnimals.Any(thingDef => thingDef == null))
            {
                Log.WarningOnce(
                    $"[NoRandomIdeologies]: Saved ideology {currentIdeo} skipped since it has venerated animals that are not loaded.",
                    filePath.GetHashCode());
                continue;
            }

            if (currentIdeo.PreferredXenotypes.Any(xenotypeDef => xenotypeDef == null))
            {
                Log.WarningOnce(
                    $"[NoRandomIdeologies]: Saved ideology {currentIdeo} skipped since it has preferred xenotypes that are not loaded.",
                    filePath.GetHashCode());
                continue;
            }

            Log.Message($"[NoRandomIdeologies]: Added {currentIdeo} to possible ideologies.");
            if (Current.Game != null)
            {
                Scribe.loader.FinalizeLoading();
                currentIdeo.fileName = Path.GetFileNameWithoutExtension(new FileInfo(filePath).Name);
                IdeoGenerator.InitLoadedIdeo(currentIdeo);
            }

            SavedIdeos.Add(currentIdeo);
        }

        isSavingOrLoadingExternalIdeoFieldInfo.SetValue(null, false);
        Scribe.ForceStop();
    }

    public static List<Ideo> FindAllValidIdeologies(FactionDef factionDef, out string toolTip)
    {
        var toolTipList = new List<string>();
        var possibleIdeos = new List<Ideo>();

        if (factionDef.fixedIdeo)
        {
            toolTip = "NRI.fixedIdeo".Translate();
            return possibleIdeos;
        }

        LoadIdeos();

        foreach (var savedIdeo in SavedIdeos)
        {
            if (NoRandomIdeologiesMod.instance.Settings.FactionIgnore.Contains(factionDef.defName))
            {
                possibleIdeos.Add(savedIdeo);
                continue;
            }

            if (savedIdeo.memes != null &&
                savedIdeo.memes.Any(def => !IdeoUtility.IsMemeAllowedFor(def, factionDef)))
            {
                toolTipList.Add("NRI.notAllowedMemes".Translate(savedIdeo));
                continue;
            }

            if (factionDef.requiredMemes != null &&
                factionDef.requiredMemes.Any(def => !savedIdeo.memes.Contains(def)))
            {
                toolTipList.Add("NRI.missingRequiredMemes".Translate(savedIdeo));
                continue;
            }

            possibleIdeos.Add(savedIdeo);
        }

        toolTip = string.Join(Environment.NewLine, toolTipList);
        return possibleIdeos;
    }

    public static bool FindIdeoForFaction(Faction faction, out Ideo ideo)
    {
        ideo = null;
        if (faction.IsPlayer)
        {
            return false;
        }

        var factionDef = faction.def;

        if (factionDef.fixedIdeo && !NoRandomIdeologiesMod.instance.Settings.FactionIgnore.Contains(factionDef.defName))
        {
            return false;
        }

        if (Find.IdeoManager.classicMode && Faction.OfPlayer != null && Faction.OfPlayer.ideos?.PrimaryIdeo != null)
        {
            return false;
        }

        LoadIdeos();

        if (!SavedIdeos.Any())
        {
            return false;
        }

        if (NoRandomIdeologiesMod.instance.Settings.PreferedIdeology.TryGetValue(factionDef.defName,
                out var selectedIdeologies) && selectedIdeologies != RandomSavedString)
        {
            if (selectedIdeologies == VanillaSaveString)
            {
                return false;
            }

            if (selectedIdeologies == PercentSaveString &&
                !Rand.Chance(NoRandomIdeologiesMod.instance.Settings.PercentChance))
            {
                return false;
            }

            var selectedIdeosSplitted = selectedIdeologies.Split(SaveStringSplitter);
            var possibleIdeos = SavedIdeos.Where(ideo => selectedIdeosSplitted.Contains(ideo.name)).ToList();
            if (possibleIdeos.Any())
            {
                ideo = possibleIdeos.RandomElement();
                return true;
            }
        }

        foreach (var savedIdeo in SavedIdeos.InRandomOrder())
        {
            ideo = savedIdeo;

            if (NoRandomIdeologiesMod.instance.Settings.FactionIgnore.Contains(factionDef.defName))
            {
                return true;
            }

            if (savedIdeo.memes != null && savedIdeo.memes.Any(def => !IdeoUtility.IsMemeAllowedFor(def, factionDef)))
            {
                continue;
            }

            if (factionDef.requiredMemes != null && factionDef.requiredMemes.Any(def => !savedIdeo.memes.Contains(def)))
            {
                continue;
            }

            return true;
        }

        return false;
    }
}