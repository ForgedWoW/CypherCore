// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Spell;

internal class CastFailed : CastFailedBase
{
    public SpellCastVisual Visual;

    public CastFailed() : base(ServerOpcodes.CastFailed, ConnectionType.Instance) { }

    public override void Write()
    {
        WorldPacket.WritePackedGuid(CastID);
        WorldPacket.WriteInt32(SpellID);

        Visual.Write(WorldPacket);

        WorldPacket.WriteInt32((int)Reason);
        WorldPacket.WriteInt32(FailedArg1);
        WorldPacket.WriteInt32(FailedArg2);
    }
}