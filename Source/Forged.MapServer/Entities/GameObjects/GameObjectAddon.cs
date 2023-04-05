// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Numerics;
using Framework.Constants;

namespace Forged.MapServer.Entities.GameObjects;

public class GameObjectAddon
{
    public uint AIAnimKitID { get; set; }
    public InvisibilityType InvisibilityType { get; set; }
    public uint InvisibilityValue { get; set; }
    public Quaternion ParentRotation { get; set; }
    public uint WorldEffectID { get; set; }
}