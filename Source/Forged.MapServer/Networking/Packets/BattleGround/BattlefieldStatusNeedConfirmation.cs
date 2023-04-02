﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.BattleGround;

public class BattlefieldStatusNeedConfirmation : ServerPacket
{
    public BattlefieldStatusHeader Hdr = new();
    public uint Mapid;
    public byte Role;
    public uint Timeout;
    public BattlefieldStatusNeedConfirmation() : base(ServerOpcodes.BattlefieldStatusNeedConfirmation) { }

    public override void Write()
    {
        Hdr.Write(_worldPacket);
        _worldPacket.WriteUInt32(Mapid);
        _worldPacket.WriteUInt32(Timeout);
        _worldPacket.WriteUInt8(Role);
    }
}