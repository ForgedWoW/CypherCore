// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;

namespace Forged.MapServer.Battlepay;

public class Purchase
{
    public uint ClientToken { get; set; }
    public ulong CurrentPrice { get; set; }
    public ulong DistributionId { get; set; }
    public bool Lock { get; set; }
    public uint ProductID { get; set; }
    public ulong PurchaseID { get; set; }
    public uint ServerToken { get; set; }
    public ushort Status { get; set; }
    public ObjectGuid TargetCharacter { get; set; } = new();
}