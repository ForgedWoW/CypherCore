// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Character;

public class BarberShopResult : ServerPacket
{
    public enum ResultEnum
    {
        Success = 0,
        NoMoney = 1,
        NotOnChair = 2,
        NoMoney2 = 3
    }

    public ResultEnum Result;

    public BarberShopResult(ResultEnum result) : base(ServerOpcodes.BarberShopResult)
    {
        Result = result;
    }

    public override void Write()
    {
        WorldPacket.WriteInt32((int)Result);
    }
}