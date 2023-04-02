// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Spell;

internal class PetCastFailed : CastFailedBase
{
    public PetCastFailed() : base(ServerOpcodes.PetCastFailed, ConnectionType.Instance) { }

    public override void Write()
    {
        WorldPacket.WritePackedGuid(CastID);
        WorldPacket.WriteInt32(SpellID);
        WorldPacket.WriteInt32((int)Reason);
        WorldPacket.WriteInt32(FailedArg1);
        WorldPacket.WriteInt32(FailedArg2);
    }
}