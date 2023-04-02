// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Movement;

public class TransferPending : ServerPacket
{
    public int MapID = -1;
    public Position OldMapPosition;
    public ShipTransferPending? Ship;
    public int? TransferSpellID;
    public TransferPending() : base(ServerOpcodes.TransferPending) { }

    public override void Write()
    {
        WorldPacket.WriteInt32(MapID);
        WorldPacket.WriteXYZ(OldMapPosition);
        WorldPacket.WriteBit(Ship.HasValue);
        WorldPacket.WriteBit(TransferSpellID.HasValue);

        if (Ship.HasValue)
        {
            WorldPacket.WriteUInt32(Ship.Value.Id);
            WorldPacket.WriteInt32(Ship.Value.OriginMapID);
        }

        if (TransferSpellID.HasValue)
            WorldPacket.WriteInt32(TransferSpellID.Value);

        WorldPacket.FlushBits();
    }

    public struct ShipTransferPending
    {
        public uint Id;         // gameobject_template.entry of the transport the player is teleporting on
        public int OriginMapID; // Map id the player is currently on (before teleport)
    }
}