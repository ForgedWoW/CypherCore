// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.


// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.Common.Networking.Packets.Talent;

struct GlyphBinding
{
	public GlyphBinding(uint spellId, ushort glyphId)
	{
		SpellID = spellId;
		GlyphID = glyphId;
	}

	public void Write(WorldPacket data)
	{
		data.WriteUInt32(SpellID);
		data.WriteUInt16(GlyphID);
	}

	readonly uint SpellID;
	readonly ushort GlyphID;
}
