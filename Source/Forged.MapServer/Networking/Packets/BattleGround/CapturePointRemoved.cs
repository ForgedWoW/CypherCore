﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.BattleGround;

internal class CapturePointRemoved : ServerPacket
{
    public ObjectGuid CapturePointGUID;

    public CapturePointRemoved() : base(ServerOpcodes.CapturePointRemoved) { }

    public CapturePointRemoved(ObjectGuid capturePointGUID) : base(ServerOpcodes.CapturePointRemoved)
    {
        CapturePointGUID = capturePointGUID;
    }

    public override void Write()
    {
        _worldPacket.WritePackedGuid(CapturePointGUID);
    }
}