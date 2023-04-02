// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;

namespace Forged.MapServer.Movement;

internal class DelayedAction
{
    private readonly Action _action;
    private readonly MotionMasterDelayedActionType _type;
    private readonly Func<bool> _validator;

    public DelayedAction(Action action, Func<bool> validator, MotionMasterDelayedActionType type)
    {
        _action = action;
        _validator = validator;
        _type = type;
    }

    public DelayedAction(Action action, MotionMasterDelayedActionType type)
    {
        _action = action;
        _validator = () => true;
        _type = type;
    }

    public void Resolve()
    {
        if (_validator())
            _action();
    }
}