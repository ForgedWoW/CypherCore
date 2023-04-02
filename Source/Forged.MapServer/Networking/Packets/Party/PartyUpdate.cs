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
        WorldPacket.WriteUInt16((ushort)PartyFlags);
        WorldPacket.WriteUInt8(PartyIndex);
        WorldPacket.WriteUInt8((byte)PartyType);
        WorldPacket.WriteInt32(MyIndex);
        WorldPacket.WritePackedGuid(PartyGUID);
        WorldPacket.WriteInt32(SequenceNum);
        WorldPacket.WritePackedGuid(LeaderGUID);
        WorldPacket.WriteUInt8(LeaderFactionGroup);
        WorldPacket.WriteInt32(PlayerList.Count);
        WorldPacket.WriteBit(LfgInfos.HasValue);
        WorldPacket.WriteBit(LootSettings.HasValue);
        WorldPacket.WriteBit(DifficultySettings.HasValue);
        WorldPacket.FlushBits();

        foreach (var playerInfo in PlayerList)
            playerInfo.Write(WorldPacket);

        LootSettings?.Write(WorldPacket);

        DifficultySettings?.Write(WorldPacket);

        LfgInfos?.Write(WorldPacket);
    }
}