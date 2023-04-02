// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Networking.Packets.Movement;

public struct MoveKnockBackSpeeds
{
    public float HorzSpeed;

    public float VertSpeed;

    public void Read(WorldPacket data)
    {
        HorzSpeed = data.ReadFloat();
        VertSpeed = data.ReadFloat();
    }

    public void Write(WorldPacket data)
    {
        data.WriteFloat(HorzSpeed);
        data.WriteFloat(VertSpeed);
    }
}