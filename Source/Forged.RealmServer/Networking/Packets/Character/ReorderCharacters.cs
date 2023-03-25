// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Forged.RealmServer.Entities;

namespace Forged.RealmServer.Networking.Packets;

public class ReorderCharacters : ClientPacket
{
	public ReorderInfo[] Entries = new ReorderInfo[200];
	public ReorderCharacters(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		var count = _worldPacket.ReadBits<uint>(9);

		for (var i = 0; i < count && i < _worldConfig.GetIntValue(WorldCfg.CharactersPerRealm); ++i)
		{
			ReorderInfo reorderInfo;
			reorderInfo.PlayerGUID = _worldPacket.ReadPackedGuid();
			reorderInfo.NewPosition = _worldPacket.ReadUInt8();
			Entries[i] = reorderInfo;
		}
	}

	public struct ReorderInfo
	{
		public ObjectGuid PlayerGUID;
		public byte NewPosition;
	}
}