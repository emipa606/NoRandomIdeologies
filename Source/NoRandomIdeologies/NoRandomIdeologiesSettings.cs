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
        Scribe_Collections.Look(ref FactionIgnore, "FactionIgnore", LookMode.Value);
        Scribe_Values.Look(ref PercentChance, "PercentChance", 0.5f);
        Scribe_Values.Look(ref DefaultSetting, "DefaultSetting", NoRandomIdeologies.RandomSavedString);
    }

    public bool CanReset()
    {
        return PreferedIdeology.Any(pair => pair.Value != DefaultSetting) || FactionIgnore.Count > 0 ||
               PercentChance != 0.5f ||
               DefaultSetting != NoRandomIdeologies.RandomSavedString;
    }

    public void Reset()
    {
        DefaultSetting = NoRandomIdeologies.RandomSavedString;
        PreferedIdeology = [];
        foreach (var keyValuePair in NoRandomIdeologies.VanillaFixedIdologies)
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