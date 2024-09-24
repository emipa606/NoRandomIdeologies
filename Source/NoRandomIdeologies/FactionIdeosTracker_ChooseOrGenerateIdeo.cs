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
            ___primaryIdeo = ideo;
            return false;
        }

        ideo.foundation.InitPrecepts(new IdeoGenerationParms(___faction.def));
        ideo.RecachePrecepts();
        ideo.primaryFactionColor = ___faction.Color;
        ___primaryIdeo = ideo;
        Find.IdeoManager.Add(ideo);
        return false;
    }
}