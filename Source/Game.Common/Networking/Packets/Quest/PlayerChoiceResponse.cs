// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Common.Networking;
using Game.Common.Networking.Packets.Quest;

namespace Game.Common.Networking.Packets.Quest;

public class PlayerChoiceResponse
{
	public int ResponseID;
	public ushort ResponseIdentifier;
	public int ChoiceArtFileID;
	public int Flags;
	public uint WidgetSetID;
	public uint UiTextureAtlasElementID;
	public uint SoundKitID;
	public byte GroupID;
	public int UiTextureKitID;
	public string Answer;
	public string Header;
	public string SubHeader;
	public string ButtonTooltip;
	public string Description;
	public string Confirmation;
	public PlayerChoiceResponseReward Reward;
	public uint? RewardQuestID;
	public PlayerChoiceResponseMawPower? MawPower;

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

		if (Reward != null)
			Reward.Write(data);

		data.WriteString(Answer);
		data.WriteString(Header);
		data.WriteString(SubHeader);
		data.WriteString(ButtonTooltip);
		data.WriteString(Description);
		data.WriteString(Confirmation);

		if (RewardQuestID.HasValue)
			data.WriteUInt32(RewardQuestID.Value);

		if (MawPower.HasValue)
			MawPower.Value.Write(data);
	}
}
