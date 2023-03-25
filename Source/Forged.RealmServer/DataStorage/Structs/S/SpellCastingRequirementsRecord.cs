// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.RealmServer.DataStorage;

public sealed class SpellCastingRequirementsRecord
{
	public uint Id;
	public uint SpellID;
	public byte FacingCasterFlags;
	public ushort MinFactionID;
	public int MinReputation;
	public ushort RequiredAreasID;
	public byte RequiredAuraVision;
	public ushort RequiresSpellFocus;
}