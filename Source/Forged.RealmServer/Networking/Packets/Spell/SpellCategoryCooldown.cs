// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;

namespace Forged.RealmServer.Networking.Packets;

public class SpellCategoryCooldown : ServerPacket
{
	public List<CategoryCooldownInfo> CategoryCooldowns = new();

	public SpellCategoryCooldown() : base(ServerOpcodes.CategoryCooldown, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WriteInt32(CategoryCooldowns.Count);

		foreach (var cooldown in CategoryCooldowns)
		{
			_worldPacket.WriteUInt32(cooldown.Category);
			_worldPacket.WriteInt32(cooldown.ModCooldown);
		}
	}

	public class CategoryCooldownInfo
	{
		public uint Category;   // SpellCategory Id
		public int ModCooldown; // Reduced Cooldown in ms

		public CategoryCooldownInfo(uint category, int cooldown)
		{
			Category = category;
			ModCooldown = cooldown;
		}
	}
}