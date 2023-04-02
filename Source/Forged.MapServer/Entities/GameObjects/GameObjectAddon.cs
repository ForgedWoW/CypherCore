// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Numerics;
using Framework.Constants;

namespace Forged.MapServer.Entities.GameObjects;

public class GameObjectAddon
{
    public uint AIAnimKitID;
    public InvisibilityType invisibilityType;
    public uint invisibilityValue;
    public Quaternion ParentRotation;
    public uint WorldEffectID;
}