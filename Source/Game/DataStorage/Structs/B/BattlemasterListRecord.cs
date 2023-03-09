// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Game.DataStorage;

public sealed class BattlemasterListRecord
{
	public uint Id;
	public LocalizedString Name;
	public string GameType;
	public string ShortDescription;
	public string LongDescription;
	public sbyte InstanceType;
	public byte MinLevel;
	public byte MaxLevel;
	public sbyte RatedPlayers;
	public sbyte MinPlayers;
	public int MaxPlayers;
	public sbyte GroupsAllowed;
	public sbyte MaxGroupSize;
	public ushort HolidayWorldState;
	public BattlemasterListFlags Flags;
	public int IconFileDataID;
	public int RequiredPlayerConditionID;
	public short[] MapId = new short[16];
}