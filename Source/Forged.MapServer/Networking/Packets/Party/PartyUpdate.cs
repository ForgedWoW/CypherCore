// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Party;

internal class PartyUpdate : ServerPacket
{
    public PartyDifficultySettings? DifficultySettings;
    public byte LeaderFactionGroup;
    public ObjectGuid LeaderGUID;
    public PartyLFGInfo? LfgInfos;
    public PartyLootSettings? LootSettings;
    public int MyIndex;
    public GroupFlags PartyFlags;
    public ObjectGuid PartyGUID;
    public byte PartyIndex;
    public GroupType PartyType;
    public List<PartyPlayerInfo> PlayerList = new();
    public int SequenceNum;
    public PartyUpdate() : base(ServerOpcodes.PartyUpdate) { }

    public override void Write()
    {
        _worldPacket.WriteUInt16((ushort)PartyFlags);
        _worldPacket.WriteUInt8(PartyIndex);
        _worldPacket.WriteUInt8((byte)PartyType);
        _worldPacket.WriteInt32(MyIndex);
        _worldPacket.WritePackedGuid(PartyGUID);
        _worldPacket.WriteInt32(SequenceNum);
        _worldPacket.WritePackedGuid(LeaderGUID);
        _worldPacket.WriteUInt8(LeaderFactionGroup);
        _worldPacket.WriteInt32(PlayerList.Count);
        _worldPacket.WriteBit(LfgInfos.HasValue);
        _worldPacket.WriteBit(LootSettings.HasValue);
        _worldPacket.WriteBit(DifficultySettings.HasValue);
        _worldPacket.FlushBits();

        foreach (var playerInfo in PlayerList)
            playerInfo.Write(_worldPacket);

        LootSettings?.Write(_worldPacket);

        DifficultySettings?.Write(_worldPacket);

        LfgInfos?.Write(_worldPacket);
    }
}