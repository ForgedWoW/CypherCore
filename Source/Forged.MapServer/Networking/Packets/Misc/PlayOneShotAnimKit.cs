﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Misc;

internal class PlayOneShotAnimKit : ServerPacket
{
    public ushort AnimKitID;
    public ObjectGuid Unit;
    public PlayOneShotAnimKit() : base(ServerOpcodes.PlayOneShotAnimKit) { }

    public override void Write()
    {
        _worldPacket.WritePackedGuid(Unit);
        _worldPacket.WriteUInt16(AnimKitID);
    }
}