// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Globals;

public class SkillTiersEntry
{
    public uint Id { get; set; }
    public uint[] Value { get; set; } = new uint[SkillConst.MaxSkillStep];
}