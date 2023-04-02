// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.DataStorage.ClientReader;

namespace Forged.MapServer.DataStorage.Structs.F;

public sealed class FactionRecord
{
    public string Description;
    public byte Expansion;
    public int Flags;
    public uint FriendshipRepID;
    public uint Id;
    public LocalizedString Name;
    public ushort ParagonFactionID;
    public byte[] ParentFactionCap = new byte[2];
    public ushort ParentFactionID;
    public float[] ParentFactionMod = new float[2];
    public int RenownCurrencyID;
    public int RenownFactionID;
    public int[] ReputationBase = new int[4];
    public short[] ReputationClassMask = new short[4];
    public ushort[] ReputationFlags = new ushort[4];
    public short ReputationIndex;
    public int[] ReputationMax = new int[4];
    public long[] ReputationRaceMask = new long[4];
    // Faction outputs rep * ParentFactionModOut as spillover reputation
    // The highest rank the faction will profit from incoming spillover

    // helpers
    public bool CanHaveReputation()
    {
        return ReputationIndex >= 0;
    }
}