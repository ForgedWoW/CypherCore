using Forged.MapServer.DataStorage.ClientReader;

namespace Forged.MapServer.DataStorage.Structs.T;

public sealed class TraitDefinitionRecord
{
    public LocalizedString OverrideName;
    public LocalizedString OverrideSubtext;
    public LocalizedString OverrideDescription;
    public uint Id;
    public uint SpellID;
    public int OverrideIcon;
    public uint OverridesSpellID;
    public uint VisibleSpellID;
}