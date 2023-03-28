using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autofac;
using Framework.Constants;
using Framework.Util;
using Microsoft.Extensions.Configuration;

namespace Forged.MapServer.Chat.Channels
{
    public class ChannelManagerFactory
    {
        private readonly IConfiguration _configuration;
        private readonly Dictionary<TeamFaction, ChannelManager> _channelManagers = new();

        public ChannelManagerFactory(ClassFactory classFactory, IConfiguration configuration)
        {
            _configuration = configuration;
            _channelManagers.Add(TeamFaction.Alliance, classFactory.Resolve<ChannelManager>(new PositionalParameter(0, TeamFaction.Alliance)));
            _channelManagers.Add(TeamFaction.Horde, classFactory.Resolve<ChannelManager>(new PositionalParameter(0, TeamFaction.Horde)));
        }

        public bool TryGetChannelManager(TeamFaction team, out ChannelManager channelManager)
        {
            return _channelManagers.TryGetValue(team, out channelManager);
        }

        public void AddChannelManager(TeamFaction team, ChannelManager channelManager)
        {
            _channelManagers[team] = channelManager;
        }

        public ChannelManager ForTeam(TeamFaction team)
        {
            if (_configuration.GetDefaultValue("AllowTwoSide.Interaction.Channel", false))
                return _channelManagers[TeamFaction.Alliance]; // cross-faction

            return TryGetChannelManager(team, out var channelManager) ? channelManager : null;
        }
    }
}
