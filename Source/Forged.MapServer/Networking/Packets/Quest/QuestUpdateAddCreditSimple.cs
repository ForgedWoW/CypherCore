// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Quest;

internal class QuestUpdateAddCreditSimple : ServerPacket
{
    public int ObjectID;
    public QuestObjectiveType ObjectiveType;
    public uint QuestID;
    public QuestUpdateAddCreditSimple() : base(ServerOpcodes.QuestUpdateAddCreditSimple, ConnectionType.Instance) { }

    public override void Write()
    {
        WorldPacket.WriteUInt32(QuestID);
        WorldPacket.WriteInt32(ObjectID);
        WorldPacket.WriteUInt8((byte)ObjectiveType);
    }
}