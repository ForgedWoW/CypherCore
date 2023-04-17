// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Misc;

internal class DisplayGameError : ServerPacket
{
    private readonly int? Arg;
    private readonly int? Arg2;
    private readonly GameError Error;

    public DisplayGameError(GameError error) : base(ServerOpcodes.DisplayGameError)
    {
        Error = error;
    }

    public DisplayGameError(GameError error, int arg) : this(error)
    {
        Arg = arg;
    }

    public DisplayGameError(GameError error, int arg1, int arg2) : this(error)
    {
        Arg = arg1;
        Arg2 = arg2;
    }

    public override void Write()
    {
        WorldPacket.WriteUInt32((uint)Error);
        WorldPacket.WriteBit(Arg.HasValue);
        WorldPacket.WriteBit(Arg2.HasValue);
        WorldPacket.FlushBits();

        if (Arg.HasValue)
            WorldPacket.WriteInt32(Arg.Value);

        if (Arg2.HasValue)
            WorldPacket.WriteInt32(Arg2.Value);
    }
}