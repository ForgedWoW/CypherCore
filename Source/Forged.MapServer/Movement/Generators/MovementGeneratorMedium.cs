// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Units;
using Framework.Constants;

namespace Forged.MapServer.Movement.Generators;

public abstract class MovementGeneratorMedium<T> : MovementGenerator where T : Unit
{
    public bool IsActive { get; set; }

    public override void Deactivate(Unit owner)
    {
        DoDeactivate((T)owner);
    }

    public abstract void DoDeactivate(T owner);

    public abstract void DoFinalize(T owner, bool active, bool movementInform);

    public abstract void DoInitialize(T owner);

    public abstract void DoReset(T owner);

    public abstract bool DoUpdate(T owner, uint diff);

    public override void Finalize(Unit owner, bool active, bool movementInform)
    {
        DoFinalize((T)owner, active, movementInform);
    }

    public override MovementGeneratorType GetMovementGeneratorType()
    {
        return MovementGeneratorType.Max;
    }

    public override void Initialize(Unit owner)
    {
        DoInitialize((T)owner);
        IsActive = true;
    }

    public override void Reset(Unit owner)
    {
        DoReset((T)owner);
    }

    public override bool Update(Unit owner, uint diff)
    {
        return DoUpdate((T)owner, diff);
    }
}