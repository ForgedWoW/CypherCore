// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Entities.GameObjects;

public class GameObjectTemplateAddon : GameObjectOverride
{
    public uint AiAnimKitId { get; set; }
    public uint[] ArtKits { get; set; } = new uint[5];
    public uint Maxgold { get; set; }
    public uint Mingold { get; set; }
    public uint WorldEffectId { get; set; }
}