// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Movement;

public struct MovementGeneratorInformation
{
    public ObjectGuid TargetGUID;
    public string TargetName;
    public MovementGeneratorType Type;

    public MovementGeneratorInformation(MovementGeneratorType type, ObjectGuid targetGUID, string targetName = "")
    {
        Type = type;
        TargetGUID = targetGUID;
        TargetName = targetName;
    }
}