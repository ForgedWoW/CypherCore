// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Guild;

public class GuildBankRemainingWithdrawMoney : ServerPacket
{
    public long RemainingWithdrawMoney;
    public GuildBankRemainingWithdrawMoney() : base(ServerOpcodes.GuildBankRemainingWithdrawMoney) { }

    public override void Write()
    {
        _worldPacket.WriteInt64(RemainingWithdrawMoney);
    }
}