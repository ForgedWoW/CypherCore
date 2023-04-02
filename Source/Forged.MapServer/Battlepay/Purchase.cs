// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;

namespace Forged.MapServer.Battlepay;

public class Purchase
{
    public uint ClientToken;
    public ulong CurrentPrice;
    public ulong DistributionId;
    public bool Lock;
    public uint ProductID;
    public ulong PurchaseID;
    public uint ServerToken;
    public ushort Status;
    public ObjectGuid TargetCharacter = new();
}