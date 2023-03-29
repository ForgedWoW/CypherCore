// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Character;

public class LogoutComplete : ServerPacket
{
    public LogoutComplete() : base(ServerOpcodes.LogoutComplete) { }

    public override void Write() { }
}