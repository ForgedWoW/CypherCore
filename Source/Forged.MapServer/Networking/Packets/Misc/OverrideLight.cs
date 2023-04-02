// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Misc;

internal class OverrideLight : ServerPacket
{
    public uint AreaLightID;
    public uint OverrideLightID;
    public uint TransitionMilliseconds;
    public OverrideLight() : base(ServerOpcodes.OverrideLight) { }

    public override void Write()
    {
        _worldPacket.WriteUInt32(AreaLightID);
        _worldPacket.WriteUInt32(OverrideLightID);
        _worldPacket.WriteUInt32(TransitionMilliseconds);
    }
}