// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Numerics;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Units;

namespace Forged.MapServer.Movement;

public class TransportPathTransform
{
    private readonly Unit _owner;
    private readonly bool _transformForTransport;

    public TransportPathTransform(Unit owner, bool transformForTransport)
    {
        _owner = owner;
        _transformForTransport = transformForTransport;
    }

    public Vector3 Calc(Vector3 input)
    {
        var pos = new Position(input);

        if (_transformForTransport)
        {
            var transport = _owner.DirectTransport;

            transport?.CalculatePassengerOffset(pos);
        }

        return pos;
    }
}