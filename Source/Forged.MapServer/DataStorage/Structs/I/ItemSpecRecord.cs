// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.DataStorage.Structs.I;

public sealed class ItemSpecRecord
{
    public uint Id;
    public byte ItemType;
    public byte MaxLevel;
    public byte MinLevel;
    public ItemSpecStat PrimaryStat;
    public ItemSpecStat SecondaryStat;
    public ushort SpecializationID;
}