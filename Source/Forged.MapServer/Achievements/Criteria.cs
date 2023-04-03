// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.DataStorage.Structs.C;
using Framework.Constants;

namespace Forged.MapServer.Achievements;

public class Criteria
{
    public CriteriaRecord Entry { get; set; }
    public CriteriaFlagsCu FlagsCu { get; set; }
    public uint Id { get; set; }
    public ModifierTreeNode Modifier { get; set; }
}