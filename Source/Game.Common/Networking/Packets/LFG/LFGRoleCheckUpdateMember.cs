// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Common.Entities.Objects;
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.


namespace Game.Common.Networking.Packets.LFG;

public class LFGRoleCheckUpdateMember
{
	public ObjectGuid Guid;
	public uint RolesDesired;
	public byte Level;
	public bool RoleCheckComplete;

	public LFGRoleCheckUpdateMember(ObjectGuid guid, uint rolesDesired, byte level, bool roleCheckComplete)
	{
		Guid = guid;
		RolesDesired = rolesDesired;
		Level = level;
		RoleCheckComplete = roleCheckComplete;
	}

	public void Write(WorldPacket data)
	{
		data.WritePackedGuid(Guid);
		data.WriteUInt32(RolesDesired);
		data.WriteUInt8(Level);
		data.WriteBit(RoleCheckComplete);
		data.FlushBits();
	}
}
