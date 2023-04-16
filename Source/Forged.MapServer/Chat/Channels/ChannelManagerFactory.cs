// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Autofac;
using Framework.Constants;
using Framework.Util;
using Game.Common;
using Microsoft.Extensions.Configuration;

namespace Forged.MapServer.Chat.Channels;

public class ChannelManagerFactory
{
    private readonly Dictionary<TeamFaction, ChannelManager> _channelManagers = new();
    private readonly IConfiguration _configuration;

    public ChannelManagerFactory(ClassFactory classFactory, IConfiguration configuration)
    {
        _configuration = configuration;
        _channelManagers.Add(TeamFaction.Alliance, classFactory.Resolve<ChannelManager>(new PositionalParameter(0, TeamFaction.Alliance)));
        _channelManagers.Add(TeamFaction.Horde, classFactory.Resolve<ChannelManager>(new PositionalParameter(0, TeamFaction.Horde)));
    }

    public void AddChannelManager(TeamFaction team, ChannelManager channelManager)
    {
        _channelManagers[team] = channelManager;
    }

    public ChannelManager ForTeam(TeamFaction team)
    {
        if (_configuration.GetDefaultValue("AllowTwoSide:Interaction:Channel", false))
            return _channelManagers[TeamFaction.Alliance]; // cross-faction

        return TryGetChannelManager(team, out var channelManager) ? channelManager : null;
    }

    public bool TryGetChannelManager(TeamFaction team, out ChannelManager channelManager)
    {
        return _channelManagers.TryGetValue(team, out channelManager);
    }
}