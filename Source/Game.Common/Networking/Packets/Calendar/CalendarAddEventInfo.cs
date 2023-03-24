// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.


namespace Game.Common.Networking.Packets.Calendar;

public class CalendarAddEventInfo
{
	public ulong ClubId;
	public string Title;
	public string Description;
	public byte EventType;
	public int TextureID;
	public long Time;
	public uint Flags;
	public CalendarAddEventInviteInfo[] Invites = new CalendarAddEventInviteInfo[(int)SharedConst.CalendarMaxInvites];

	public void Read(WorldPacket data)
	{
		ClubId = data.ReadUInt64();
		EventType = data.ReadUInt8();
		TextureID = data.ReadInt32();
		Time = data.ReadPackedTime();
		Flags = data.ReadUInt32();
		var InviteCount = data.ReadUInt32();

		var titleLength = data.ReadBits<byte>(8);
		var descriptionLength = data.ReadBits<ushort>(11);

		for (var i = 0; i < InviteCount; ++i)
		{
			CalendarAddEventInviteInfo invite = new();
			invite.Read(data);
			Invites[i] = invite;
		}

		Title = data.ReadString(titleLength);
		Description = data.ReadString(descriptionLength);
	}
}
