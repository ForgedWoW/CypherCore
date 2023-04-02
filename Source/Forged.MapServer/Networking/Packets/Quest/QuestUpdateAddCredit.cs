// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Quest;

public class QuestUpdateAddCredit : ServerPacket
{
    public ushort Count;
    public int ObjectID;
    public byte ObjectiveType;
    public uint QuestID;
    public ushort Required;
    public ObjectGuid VictimGUID;
    public QuestUpdateAddCredit() : base(ServerOpcodes.QuestUpdateAddCredit, ConnectionType.Instance) { }

    public override void Write()
    {
        WorldPacket.WritePackedGuid(VictimGUID);
        WorldPacket.WriteUInt32(QuestID);
        WorldPacket.WriteInt32(ObjectID);
        WorldPacket.WriteUInt16(Count);
        WorldPacket.WriteUInt16(Required);
        WorldPacket.WriteUInt8(ObjectiveType);
    }
}