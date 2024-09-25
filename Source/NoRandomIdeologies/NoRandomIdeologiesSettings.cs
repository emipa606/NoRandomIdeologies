using System.Collections.Generic;
using Verse;

namespace NoRandomIdeologies;

/// <summary>
///     Definition of the settings for the mod
/// </summary>
internal class NoRandomIdeologiesSettings : ModSettings
{
    public Dictionary<string, string> PreferedIdeology = [];
    private List<string> preferedIdeologyKeys = [];
    private List<string> preferedIdeologyValues = [];

    /// <summary>
    ///     Saving and loading the values
    /// </summary>
    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Collections.Look(ref PreferedIdeology, "PreferedIdeology", LookMode.Value, LookMode.Value,
            ref preferedIdeologyKeys, ref preferedIdeologyValues);
    }
}