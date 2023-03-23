// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Entities;
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Common.Entities.Objects.Update;
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Common.Networking;
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Common.Networking.Packets.Spell;

namespace Game.Common.Networking.Packets.Spell;

public struct SpellCastVisual
{
	public uint SpellXSpellVisualID;
	public uint ScriptVisualID;

	public SpellCastVisual(uint spellXSpellVisualID, uint scriptVisualID)
	{
		SpellXSpellVisualID = spellXSpellVisualID;
		ScriptVisualID = scriptVisualID;
	}

	public void Read(WorldPacket data)
	{
		SpellXSpellVisualID = data.ReadUInt32();
		ScriptVisualID = data.ReadUInt32();
	}

	public void Write(WorldPacket data)
	{
		data.WriteUInt32(SpellXSpellVisualID);
		data.WriteUInt32(ScriptVisualID);
	}

	public static implicit operator SpellCastVisualField(SpellCastVisual spellCastVisual)
	{
		SpellCastVisualField visual = new();
		visual.SpellXSpellVisualID = spellCastVisual.SpellXSpellVisualID;
		visual.ScriptVisualID = spellCastVisual.ScriptVisualID;

		return visual;
	}
}
