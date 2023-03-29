﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Networking.Packets.Bpay;

public sealed class BattlePayRequestPriceInfo : ClientPacket
{
    public byte UnkByte { get; set; } = 0;

    public BattlePayRequestPriceInfo(WorldPacket packet) : base(packet) { }

    public override void Read() { }
}