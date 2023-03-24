// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;

namespace Game.Common.Networking.Packets.Spell;

public class SendKnownSpells : ServerPacket
{
	public bool InitialLogin;
	public List<uint> KnownSpells = new();
	public List<uint> FavoriteSpells = new(); // tradeskill recipes
	public SendKnownSpells() : base(ServerOpcodes.SendKnownSpells, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WriteBit(InitialLogin);
		_worldPacket.WriteInt32(KnownSpells.Count);
		_worldPacket.WriteInt32(FavoriteSpells.Count);

		foreach (var spellId in KnownSpells)
			_worldPacket.WriteUInt32(spellId);

		foreach (var spellId in FavoriteSpells)
			_worldPacket.WriteUInt32(spellId);
	}
}
