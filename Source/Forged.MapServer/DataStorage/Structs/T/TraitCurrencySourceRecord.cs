// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.DataStorage.ClientReader;

namespace Forged.MapServer.DataStorage.Structs.T;

public sealed record TraitCurrencySourceRecord
{
    public uint AchievementID;
    public int Amount;
    public uint Id;
    public int OrderIndex;
    public uint PlayerLevel;
    public uint QuestID;
    public LocalizedString Requirement;
    public int TraitCurrencyID;
    public int TraitNodeEntryID;
}