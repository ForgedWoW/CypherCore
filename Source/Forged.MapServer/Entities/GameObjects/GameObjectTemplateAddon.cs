// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Entities.GameObjects;

public class GameObjectTemplateAddon : GameObjectOverride
{
	public uint Mingold;
	public uint Maxgold;
	public uint[] ArtKits = new uint[5];
	public uint WorldEffectId;
	public uint AiAnimKitId;
}