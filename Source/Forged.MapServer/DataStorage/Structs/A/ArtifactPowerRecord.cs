// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Numerics;
using Framework.Constants;

namespace Forged.MapServer.DataStorage.Structs.A;

public sealed record ArtifactPowerRecord
{
    public byte ArtifactID;
    public Vector2 DisplayPos;
    public ArtifactPowerFlag Flags;
    public uint Id;
    public int Label;
    public byte MaxPurchasableRank;
    public byte Tier;
}