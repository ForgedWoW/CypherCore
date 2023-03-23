// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Entities;

namespace Forged.RealmServer.Battlepay;

public class Purchase
{
	public ObjectGuid TargetCharacter = new();
	public ulong DistributionId;
	public ulong PurchaseID;
	public ulong CurrentPrice;
	public uint ClientToken;
	public uint ServerToken;
	public uint ProductID;
	public ushort Status;
	public bool Lock;
}