// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.DataStorage.Structs.M;

public sealed record MountXDisplayRecord
{
    public uint CreatureDisplayInfoID;
    public uint Id;
    public uint MountID;
    public uint PlayerConditionID;
}