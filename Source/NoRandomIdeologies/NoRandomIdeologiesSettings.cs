using System.Collections.Generic;
using System.Linq;
using Verse;

namespace NoRandomIdeologies;

/// <summary>
///     Definition of the settings for the mod
/// </summary>
internal class NoRandomIdeologiesSettings : ModSettings
{
    public string DefaultSetting = NoRandomIdeologies.RandomSavedString;
    public List<string> FactionIgnore = [];
    public float PercentChance = 0.5f;
    public Dictionary<string, string> PreferredIdeology = [];
    private List<string> preferredIdeologyKeys = [];
    private List<string> preferredIdeologyValues = [];

    /// <summary>
    ///     Saving and loading the values
    /// </summary>
    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Collections.Look(ref PreferredIdeology, "PreferedIdeology", LookMode.Value, LookMode.Value,
            ref preferredIdeologyKeys, ref preferredIdeologyValues);
        Scribe_Collections.Look(ref FactionIgnore, "FactionIgnore", LookMode.Value);
        Scribe_Values.Look(ref PercentChance, "PercentChance", 0.5f);
        Scribe_Values.Look(ref DefaultSetting, "DefaultSetting", NoRandomIdeologies.RandomSavedString);
    }

    public bool CanReset()
    {
        return PreferredIdeology.Any(pair => pair.Value != DefaultSetting) || FactionIgnore.Count > 0 ||
               PercentChance != 0.5f ||
               DefaultSetting != NoRandomIdeologies.RandomSavedString;
    }

    public void Reset()
    {
        DefaultSetting = NoRandomIdeologies.RandomSavedString;
        PreferredIdeology = [];
        foreach (var keyValuePair in NoRandomIdeologies.VanillaFixedIdeologies)
        {
            keyValuePair.Key.fixedIdeo = keyValuePair.Value;
        }

        foreach (var keyValuePair in NoRandomIdeologies.VanillaRequiredMemes)
        {
            keyValuePair.Key.requiredMemes = keyValuePair.Value;
        }

        FactionIgnore = [];
        PercentChance = 0.5f;
    }
}