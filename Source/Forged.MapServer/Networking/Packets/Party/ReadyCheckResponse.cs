// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Party;

internal class ReadyCheckResponse : ServerPacket
{
    public ObjectGuid PartyGUID;
    public ObjectGuid Player;
    public bool IsReady;
    public ReadyCheckResponse() : base(ServerOpcodes.ReadyCheckResponse) { }

    public override void Write()
    {
        _worldPacket.WritePackedGuid(PartyGUID);
        _worldPacket.WritePackedGuid(Player);

        _worldPacket.WriteBit(IsReady);
        _worldPacket.FlushBits();
    }
}