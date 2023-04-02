// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Instance;

internal class InstanceEncounterChangePriority : ServerPacket
{
    public byte TargetFramePriority;
    public ObjectGuid Unit;
    // used to update the position of the unit's current frame
    public InstanceEncounterChangePriority() : base(ServerOpcodes.InstanceEncounterChangePriority, ConnectionType.Instance) { }

    public override void Write()
    {
        _worldPacket.WritePackedGuid(Unit);
        _worldPacket.WriteUInt8(TargetFramePriority);
    }
}