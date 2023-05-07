// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Numerics;
using Framework.Constants;

namespace Forged.MapServer.DataStorage.Structs.T;

public sealed record TaxiPathNodeRecord
{
    public uint ArrivalEventID;
    public ushort ContinentID;
    public uint Delay;
    public uint DepartureEventID;
    public TaxiPathNodeFlags Flags;
    public uint Id;
    public Vector3 Loc;
    public int NodeIndex;
    public ushort PathID;
}