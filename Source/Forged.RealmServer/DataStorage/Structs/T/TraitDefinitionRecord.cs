// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.RealmServer.DataStorage;

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