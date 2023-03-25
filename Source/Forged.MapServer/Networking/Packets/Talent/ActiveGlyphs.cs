// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;

namespace Game.Networking.Packets;

class ActiveGlyphs : ServerPacket
{
	public List<GlyphBinding> Glyphs = new();
	public bool IsFullUpdate;
	public ActiveGlyphs() : base(ServerOpcodes.ActiveGlyphs) { }

	public override void Write()
	{
		_worldPacket.WriteInt32(Glyphs.Count);

		foreach (var glyph in Glyphs)
			glyph.Write(_worldPacket);

		_worldPacket.WriteBit(IsFullUpdate);
		_worldPacket.FlushBits();
	}
}