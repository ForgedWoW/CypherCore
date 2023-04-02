// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.DataStorage.Structs.I;

public sealed class ItemBonusTreeNodeRecord
{
    public uint ChildItemBonusListGroupID;
    public ushort ChildItemBonusListID;
    public ushort ChildItemBonusTreeID;
    public ushort ChildItemLevelSelectorID;
    public uint IblGroupPointsModSetID;
    public uint Id;
    public byte ItemContext;
    public uint ParentItemBonusTreeID;
}