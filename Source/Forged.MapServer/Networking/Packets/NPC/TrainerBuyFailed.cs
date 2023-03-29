// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.NPC;

internal class TrainerBuyFailed : ServerPacket
{
    public ObjectGuid TrainerGUID;
    public uint SpellID;
    public TrainerFailReason TrainerFailedReason;
    public TrainerBuyFailed() : base(ServerOpcodes.TrainerBuyFailed) { }

    public override void Write()
    {
        _worldPacket.WritePackedGuid(TrainerGUID);
        _worldPacket.WriteUInt32(SpellID);
        _worldPacket.WriteUInt32((uint)TrainerFailedReason);
    }
}