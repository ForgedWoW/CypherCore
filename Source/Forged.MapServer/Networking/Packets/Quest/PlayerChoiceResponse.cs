// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;

namespace Forged.MapServer.Networking.Packets.Quest;

internal class PlayerChoiceResponse
{
    public string Answer;
    public string ButtonTooltip;
    public int ChoiceArtFileID;
    public string Confirmation;
    public string Description;
    public int Flags;
    public byte GroupID;
    public string Header;
    public PlayerChoiceResponseMawPower? MawPower;
    public int ResponseID;
    public ushort ResponseIdentifier;
    public PlayerChoiceResponseReward Reward;
    public uint? RewardQuestID;
    public uint SoundKitID;
    public string SubHeader;
    public uint UiTextureAtlasElementID;
    public int UiTextureKitID;
    public uint WidgetSetID;
    public void Write(WorldPacket data)
    {
        data.WriteInt32(ResponseID);
        data.WriteUInt16(ResponseIdentifier);
        data.WriteInt32(ChoiceArtFileID);
        data.WriteInt32(Flags);
        data.WriteUInt32(WidgetSetID);
        data.WriteUInt32(UiTextureAtlasElementID);
        data.WriteUInt32(SoundKitID);
        data.WriteUInt8(GroupID);
        data.WriteInt32(UiTextureKitID);

        data.WriteBits(Answer.GetByteCount(), 9);
        data.WriteBits(Header.GetByteCount(), 9);
        data.WriteBits(SubHeader.GetByteCount(), 7);
        data.WriteBits(ButtonTooltip.GetByteCount(), 9);
        data.WriteBits(Description.GetByteCount(), 11);
        data.WriteBits(Confirmation.GetByteCount(), 7);

        data.WriteBit(RewardQuestID.HasValue);
        data.WriteBit(Reward != null);
        data.WriteBit(MawPower.HasValue);
        data.FlushBits();

        Reward?.Write(data);

        data.WriteString(Answer);
        data.WriteString(Header);
        data.WriteString(SubHeader);
        data.WriteString(ButtonTooltip);
        data.WriteString(Description);
        data.WriteString(Confirmation);

        if (RewardQuestID.HasValue)
            data.WriteUInt32(RewardQuestID.Value);

        MawPower?.Write(data);
    }
}