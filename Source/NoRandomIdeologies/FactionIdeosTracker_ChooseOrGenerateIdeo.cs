using HarmonyLib;
using RimWorld;
using Verse;

namespace NoRandomIdeologies;

[HarmonyPatch(typeof(FactionIdeosTracker), nameof(FactionIdeosTracker.ChooseOrGenerateIdeo))]
public static class FactionIdeosTracker_ChooseOrGenerateIdeo
{
    public static bool Prefix(ref Ideo ___primaryIdeo, Faction ___faction)
    {
        if (!NoRandomIdeologies.FindIdeoForFaction(___faction, out var ideo))
        {
            return true;
        }

        var existingIdeo = Find.IdeoManager.IdeosListForReading?.FirstOrDefault(ideology => ideology.name == ideo.name);
        if (existingIdeo != null)
        {
            if (Prefs.DevMode)
            {
                Log.Message($"[NoRandomIdeologies]: Gave existing ideology {existingIdeo} to {___faction}");
            }

            ___primaryIdeo = existingIdeo;
            return false;
        }

        ___primaryIdeo = ideo;
        Find.IdeoManager.Add(ideo);
        Log.Message($"[NoRandomIdeologies]: Gave ideology {ideo} to {___faction}");
        return false;
    }
}