// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.DataStorage.ClientReader;

namespace Forged.MapServer.DataStorage.Structs.A;

public sealed class AdventureJournalRecord
{
    public ushort BattleMasterListID;
    public uint[] BonusPlayerConditionID = new uint[2];
    public byte[] BonusValue = new byte[2];
    public byte ButtonActionType;
    public string ButtonText;
    public string ContinueDescription;
    public uint CurrencyQuantity;
    public ushort CurrencyType;
    public string Description;
    public int Flags;
    public uint Id;
    public int ItemID;
    public uint ItemQuantity;
    public ushort LfgDungeonID;
    public LocalizedString Name;
    public uint PlayerConditionID;
    public byte PriorityMax;
    public byte PriorityMin;
    public uint QuestID;
    public string RewardDescription;
    public int TextureFileDataID;
    public byte Type;
    public ushort UiMapID;
}