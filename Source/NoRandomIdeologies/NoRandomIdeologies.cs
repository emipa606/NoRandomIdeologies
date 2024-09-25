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
    private static DateTime lastCheck;
    private static readonly List<Ideo> savedIdeos = [];

    private static readonly FieldInfo isSavingOrLoadingExternalIdeoFieldInfo =
        AccessTools.Field(typeof(GameDataSaveLoader), "isSavingOrLoadingExternalIdeo");

    private static readonly FieldInfo reachedMaxMessagesLimitFieldInfo =
        AccessTools.Field(typeof(Log), "reachedMaxMessagesLimit");

    public static readonly List<FactionDef> AllFactionDefs;

    static NoRandomIdeologies()
    {
        AllFactionDefs = DefDatabase<FactionDef>.AllDefsListForReading.Where(def => !def.isPlayer)
            .OrderBy(def => def.label).ToList();
        new Harmony("Mlie.NoRandomIdeologies").PatchAll(Assembly.GetExecutingAssembly());
    }

    private static void LoadIdeos()
    {
        if (!GenFilePaths.AllCustomIdeoFiles.Any())
        {
            Log.WarningOnce("[NoRandomIdeologies]: No saved ideologies found.",
                "No saved ideologies found".GetHashCode());
            return;
        }

        var lastWriteTime = GenFilePaths.AllCustomIdeoFiles.OrderByDescending(info => info.LastWriteTime).First()
            .LastWriteTime;
        if (lastCheck == lastWriteTime)
        {
            return;
        }

        NoRandomIdeologiesMod.FactionSelectionCache.Clear();
        lastCheck = lastWriteTime;

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

        savedIdeos.Clear();
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
            Scribe.loader.FinalizeLoading();
            currentIdeo.fileName = Path.GetFileNameWithoutExtension(new FileInfo(filePath).Name);
            IdeoGenerator.InitLoadedIdeo(currentIdeo);
            savedIdeos.Add(currentIdeo);
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

        foreach (var savedIdeo in savedIdeos)
        {
            if (savedIdeo.memes != null && savedIdeo.memes.Any(def => !IdeoUtility.IsMemeAllowedFor(def, factionDef)))
            {
                toolTipList.Add("NRI.notAllowedMemes".Translate(savedIdeo));
                continue;
            }

            if (factionDef.requiredMemes != null && factionDef.requiredMemes.Any(def => !savedIdeo.memes.Contains(def)))
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

        if (factionDef.fixedIdeo)
        {
            return false;
        }

        if (Find.IdeoManager.classicMode && Faction.OfPlayer != null && Faction.OfPlayer.ideos?.PrimaryIdeo != null)
        {
            return false;
        }

        LoadIdeos();

        if (!savedIdeos.Any())
        {
            return false;
        }

        if (NoRandomIdeologiesMod.instance.Settings.PreferedIdeology.TryGetValue(factionDef.defName,
                out var selectedIdeology))
        {
            var selectedIdeo = savedIdeos.FirstOrDefault(ideo => ideo.name == selectedIdeology);
            if (selectedIdeo != null)
            {
                ideo = selectedIdeo;
                return true;
            }
        }

        foreach (var savedIdeo in savedIdeos.InRandomOrder())
        {
            if (savedIdeo.memes != null && savedIdeo.memes.Any(def => !IdeoUtility.IsMemeAllowedFor(def, factionDef)))
            {
                continue;
            }

            if (factionDef.requiredMemes != null && factionDef.requiredMemes.Any(def => !savedIdeo.memes.Contains(def)))
            {
                continue;
            }

            ideo = savedIdeo;

            return true;
        }

        return false;
    }
}