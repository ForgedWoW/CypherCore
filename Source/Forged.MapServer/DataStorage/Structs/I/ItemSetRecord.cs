// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.DataStorage.ClientReader;
using Framework.Constants;

namespace Forged.MapServer.DataStorage.Structs.I;

public sealed class ItemSetRecord
{
    public uint Id;
    public LocalizedString Name;
    public ItemSetFlags SetFlags;
    public uint RequiredSkill;
    public ushort RequiredSkillRank;
    public uint[] ItemID = new uint[ItemConst.MaxItemSetItems];
}