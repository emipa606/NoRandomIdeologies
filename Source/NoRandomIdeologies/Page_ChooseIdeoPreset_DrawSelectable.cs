using HarmonyLib;
using RimWorld;
using Verse;

namespace NoRandomIdeologies;

[HarmonyPatch(typeof(Page_ChooseIdeoPreset), "AssignIdeoToPlayer")]
public static class Page_ChooseIdeoPreset_DrawSelectable
{
    public static bool Prefix(Ideo ideo)
    {
        var existingIdeo = Find.IdeoManager.IdeosListForReading.FirstOrDefault(ideology => ideology.name == ideo.name);
        if (existingIdeo == null)
        {
            return true;
        }

        Faction.OfPlayer.ideos.SetPrimary(existingIdeo);
        existingIdeo.initialPlayerIdeo = true;
        return false;
    }
}