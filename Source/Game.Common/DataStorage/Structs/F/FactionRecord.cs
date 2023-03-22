// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.DataStorage;

public sealed class FactionRecord
{
	public uint Id;
	public long[] ReputationRaceMask = new long[4];
	public LocalizedString Name;
	public string Description;
	public short ReputationIndex;
	public ushort ParentFactionID;
	public byte Expansion;
	public uint FriendshipRepID;
	public int Flags;
	public ushort ParagonFactionID;
	public int RenownFactionID;
	public int RenownCurrencyID;
	public short[] ReputationClassMask = new short[4];
	public ushort[] ReputationFlags = new ushort[4];
	public int[] ReputationBase = new int[4];
	public int[] ReputationMax = new int[4];
	public float[] ParentFactionMod = new float[2]; // Faction outputs rep * ParentFactionModOut as spillover reputation
	public byte[] ParentFactionCap = new byte[2];   // The highest rank the faction will profit from incoming spillover

	// helpers
	public bool CanHaveReputation()
	{
		return ReputationIndex >= 0;
	}
}