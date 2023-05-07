// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.DataStorage.Structs.Q;

public sealed record QuestPackageItemRecord
{
    public QuestPackageFilter DisplayType;
    public uint Id;
    public uint ItemID;
    public byte ItemQuantity;
    public ushort PackageID;
}