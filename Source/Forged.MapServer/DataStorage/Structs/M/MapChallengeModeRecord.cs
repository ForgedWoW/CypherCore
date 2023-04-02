// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.DataStorage.ClientReader;

namespace Forged.MapServer.DataStorage.Structs.M;

public sealed class MapChallengeModeRecord
{
    public short[] CriteriaCount = new short[3];
    public uint ExpansionLevel;
    public byte Flags;
    public uint Id;
    public ushort MapID;
    public LocalizedString Name;
    public int RequiredWorldStateID; // maybe?
}