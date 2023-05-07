// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.DataStorage.ClientReader;

namespace Forged.MapServer.DataStorage.Structs.T;

public sealed record TraitDefinitionRecord
{
    public uint Id;
    public LocalizedString OverrideDescription;
    public int OverrideIcon;
    public LocalizedString OverrideName;
    public uint OverridesSpellID;
    public LocalizedString OverrideSubtext;
    public uint SpellID;
    public uint VisibleSpellID;
}