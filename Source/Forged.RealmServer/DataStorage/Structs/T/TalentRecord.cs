// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.RealmServer.DataStorage;

public sealed class TalentRecord
{
	public uint Id;
	public string Description;
	public byte TierID;
	public byte Flags;
	public byte ColumnIndex;
	public byte ClassID;
	public ushort SpecID;
	public uint SpellID;
	public uint OverridesSpellID;
	public byte[] CategoryMask = new byte[2];
}