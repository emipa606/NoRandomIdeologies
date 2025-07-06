using HarmonyLib;
using RimWorld;

namespace NoRandomIdeologies;

[HarmonyPatch(typeof(IdeoUtility), nameof(IdeoUtility.IsMemeAllowedFor))]
public static class IdeoUtility_IsMemeAllowedFor
{
    public static bool Prefix(FactionDef faction, ref bool __result)
    {
        if (!NoRandomIdeologiesMod.Instance.Settings.FactionIgnore.Contains(faction.defName))
        {
            return true;
        }

        __result = true;
        return false;
    }
}