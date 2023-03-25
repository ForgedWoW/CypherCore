// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.RealmServer.DataStorage;

public sealed class AdventureJournalRecord
{
	public uint Id;
	public LocalizedString Name;
	public string Description;
	public string ButtonText;
	public string RewardDescription;
	public string ContinueDescription;
	public byte Type;
	public uint PlayerConditionID;
	public int Flags;
	public byte ButtonActionType;
	public int TextureFileDataID;
	public ushort LfgDungeonID;
	public uint QuestID;
	public ushort BattleMasterListID;
	public byte PriorityMin;
	public byte PriorityMax;
	public int ItemID;
	public uint ItemQuantity;
	public ushort CurrencyType;
	public uint CurrencyQuantity;
	public ushort UiMapID;
	public uint[] BonusPlayerConditionID = new uint[2];
	public byte[] BonusValue = new byte[2];
}