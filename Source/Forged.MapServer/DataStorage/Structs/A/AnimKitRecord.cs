// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.DataStorage.Structs.A;

public sealed record AnimKitRecord
{
    public uint Id;
    public ushort LowDefAnimKitID;
    public uint OneShotDuration;
    public ushort OneShotStopAnimKitID;
}