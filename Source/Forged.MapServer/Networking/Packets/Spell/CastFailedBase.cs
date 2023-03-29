// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Spell;

internal class CastFailedBase : ServerPacket
{
    public ObjectGuid CastID;
    public int SpellID;
    public SpellCastResult Reason;
    public int FailedArg1 = -1;
    public int FailedArg2 = -1;

    public CastFailedBase(ServerOpcodes opcode, ConnectionType connectionType) : base(opcode, connectionType) { }

    public override void Write()
    {
        throw new NotImplementedException();
    }
}