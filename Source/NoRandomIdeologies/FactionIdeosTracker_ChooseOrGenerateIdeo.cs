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

        if (Find.IdeoManager.IdeosListForReading?.Contains(ideo) == true)
        {
            if (Prefs.DevMode)
            {
                Log.Message($"[NoRandomIdeologies]: Gave existing ideology {ideo} to {___faction}");
            }

            ___primaryIdeo = ideo;
            return false;
        }

        ___primaryIdeo = ideo;
        Find.IdeoManager.Add(ideo);
        Log.Message($"[NoRandomIdeologies]: Gave ideology {ideo} to {___faction}");
        return false;
    }
}