// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.VoidStorage;

internal class VoidStorageTransfer : ClientPacket
{
    public ObjectGuid[] Deposits = new ObjectGuid[(int)SharedConst.VoidStorageMaxDeposit];
    public ObjectGuid Npc;
    public ObjectGuid[] Withdrawals = new ObjectGuid[(int)SharedConst.VoidStorageMaxWithdraw];
    public VoidStorageTransfer(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        Npc = WorldPacket.ReadPackedGuid();
        var DepositCount = WorldPacket.ReadInt32();
        var WithdrawalCount = WorldPacket.ReadInt32();

        for (uint i = 0; i < DepositCount; ++i)
            Deposits[i] = WorldPacket.ReadPackedGuid();

        for (uint i = 0; i < WithdrawalCount; ++i)
            Withdrawals[i] = WorldPacket.ReadPackedGuid();
    }
}