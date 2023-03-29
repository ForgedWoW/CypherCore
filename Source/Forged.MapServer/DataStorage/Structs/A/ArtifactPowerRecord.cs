// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Numerics;
using Framework.Constants;

namespace Forged.MapServer.DataStorage.Structs.A;

public sealed class ArtifactPowerRecord
{
    public Vector2 DisplayPos;
    public uint Id;
    public byte ArtifactID;
    public byte MaxPurchasableRank;
    public int Label;
    public ArtifactPowerFlag Flags;
    public byte Tier;
}