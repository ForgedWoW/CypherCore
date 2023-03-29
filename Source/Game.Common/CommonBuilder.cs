// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Autofac;

namespace Game.Common;

public static class CommonBuilder
{
    public static ContainerBuilder AddCommon(this ContainerBuilder builder)
    {
        return builder;
    }

    public static ContainerBuilder AddSessionCommon(this ContainerBuilder builder)
    {
        return builder;
    }
}