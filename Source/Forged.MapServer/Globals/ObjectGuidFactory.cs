// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Autofac;
using Forged.MapServer.Arenas;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Guilds;
using Framework.Constants;
using Game.Common;

namespace Forged.MapServer.Globals;

public class ObjectGuidGeneratorFactory
{
    private readonly ClassFactory _classFactory;
    private readonly Dictionary<HighGuid, ObjectGuidGenerator> _guidGenerators = new();

    public ObjectGuidGeneratorFactory(ClassFactory classFactory)
    {
        _classFactory = classFactory;
    }

    public ObjectGuidGenerator GetGenerator(HighGuid high)
    {
        if (_guidGenerators.TryGetValue(high, out var generator))
            return generator;

        generator = _classFactory.ResolveWithPositionalParameters<ObjectGuidGenerator>(high, 1);
        _guidGenerators[high] = generator;

        return generator;
    }
}