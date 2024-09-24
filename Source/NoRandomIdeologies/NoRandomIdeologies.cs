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

    static NoRandomIdeologies()
    {
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
                    "This may cause errors if any saved ideology have precepts or memes that uses content from not loaded mods. " +
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
                Scribe_Deep.Look(ref currentIdeo, "ideo");
            }
            catch
            {
                Log.WarningOnce($"[NoRandomIdeologies]: Skipping {filePath} since it couldnt be loaded.",
                    filePath.GetHashCode());
                continue;
            }

            if (currentIdeo == null)
            {
                Log.WarningOnce($"[NoRandomIdeologies]: Skipping {filePath} since it couldnt be loaded.",
                    filePath.GetHashCode());
                continue;
            }

            if (!currentIdeo.PreceptsListForReading.Any() || currentIdeo.PreceptsListForReading.Any(precept =>
                    precept == null))
            {
                Log.WarningOnce(
                    $"[NoRandomIdeologies]: Skipping {currentIdeo} since it has a precepts that is not loaded.",
                    filePath.GetHashCode());
                continue;
            }

            if (!currentIdeo.memes.Any() || currentIdeo.memes.Any(memeDef => memeDef == null))
            {
                Log.WarningOnce(
                    $"[NoRandomIdeologies]: Skipping {currentIdeo} since it has memes that are not loaded.",
                    filePath.GetHashCode());
                continue;
            }

            if (currentIdeo.VeneratedAnimals.Any(thingDef => thingDef == null))
            {
                Log.WarningOnce(
                    $"[NoRandomIdeologies]: Skipping {currentIdeo} since it has venerated animals that are not loaded.",
                    filePath.GetHashCode());
                continue;
            }

            if (currentIdeo.PreferredXenotypes.Any(xenotypeDef => xenotypeDef == null))
            {
                Log.WarningOnce(
                    $"[NoRandomIdeologies]: Skipping {currentIdeo} since it has preferred xenotypes that are not loaded.",
                    filePath.GetHashCode());
                continue;
            }

            Log.Message($"[NoRandomIdeologies]: Adding {currentIdeo} to possible ideologies.");
            Scribe.loader.FinalizeLoading();

            savedIdeos.Add(currentIdeo);
        }

        isSavingOrLoadingExternalIdeoFieldInfo.SetValue(null, false);
        Scribe.ForceStop();
    }

    public static bool FindIdeoForFaction(Faction faction, out Ideo ideo)
    {
        ideo = null;
        if (faction.IsPlayer)
        {
            if (Prefs.DevMode)
            {
                Log.WarningOnce($"[NoRandomIdeologies]: Skipping {faction} since its the player faction.",
                    faction.GetHashCode());
            }

            return false;
        }

        var factionDef = faction.def;

        if (factionDef.fixedIdeo)
        {
            if (Prefs.DevMode)
            {
                Log.WarningOnce($"[NoRandomIdeologies]: Skipping {factionDef} since it has a fixed ideology.",
                    factionDef.GetHashCode());
            }

            return false;
        }

        if (Find.IdeoManager.classicMode && Faction.OfPlayer != null && Faction.OfPlayer.ideos?.PrimaryIdeo != null)
        {
            if (Prefs.DevMode)
            {
                Log.WarningOnce("[NoRandomIdeologies]: Classic mode selected, no custom ideologies allowed.",
                    "no custom ideologies allowed".GetHashCode());
            }

            return false;
        }

        LoadIdeos();

        if (!savedIdeos.Any())
        {
            return false;
        }

        foreach (var savedIdeo in savedIdeos.InRandomOrder())
        {
            if (savedIdeo.memes != null && savedIdeo.memes.Any(def => !IdeoUtility.IsMemeAllowedFor(def, factionDef)))
            {
                if (Prefs.DevMode)
                {
                    Log.WarningOnce($"[NoRandomIdeologies]: {savedIdeo} has memes not allowed by {factionDef}.",
                        $"{savedIdeo}{factionDef}".GetHashCode());
                }

                continue;
            }

            if (factionDef.requiredMemes != null && factionDef.requiredMemes.Any(def => !savedIdeo.memes.Contains(def)))
            {
                if (Prefs.DevMode)
                {
                    Log.WarningOnce($"[NoRandomIdeologies]: {factionDef} has required memes not in {savedIdeo}.",
                        $"{savedIdeo}{factionDef}".GetHashCode());
                }

                continue;
            }

            ideo = savedIdeo;

            return true;
        }

        return false;
    }
}