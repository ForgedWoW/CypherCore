// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.DataStorage.ClientReader;
using Framework.Constants;

namespace Forged.MapServer.DataStorage.Structs.B;

public sealed class BattlemasterListRecord
{
    public BattlemasterListFlags Flags;
    public string GameType;
    public sbyte GroupsAllowed;
    public ushort HolidayWorldState;
    public int IconFileDataID;
    public uint Id;
    public sbyte InstanceType;
    public string LongDescription;
    public short[] MapId = new short[16];
    public sbyte MaxGroupSize;
    public byte MaxLevel;
    public int MaxPlayers;
    public byte MinLevel;
    public sbyte MinPlayers;
    public LocalizedString Name;
    public sbyte RatedPlayers;
    public int RequiredPlayerConditionID;
    public string ShortDescription;
}