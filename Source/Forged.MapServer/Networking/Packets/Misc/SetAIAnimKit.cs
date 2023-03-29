// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Misc;

internal class SetAIAnimKit : ServerPacket
{
    public ObjectGuid Unit;
    public ushort AnimKitID;
    public SetAIAnimKit() : base(ServerOpcodes.SetAiAnimKit, ConnectionType.Instance) { }

    public override void Write()
    {
        _worldPacket.WritePackedGuid(Unit);
        _worldPacket.WriteUInt16(AnimKitID);
    }
}