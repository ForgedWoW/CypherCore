// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.System;

public class FeatureSystemStatusGlueScreen : ServerPacket
{
    public int ActiveCharacterUpgradeBoostType;
    // NYI
    public int ActiveClassTrialBoostType;

    public int ActiveSeason;
    public bool AddonsDisabled;
    public bool BpayStoreAvailable;                  // NYI
    public bool BpayStoreDisabledByParentalControls; // NYI
    public bool BpayStoreEnabled;
    public uint BpayStoreProductDeliveryDelay;
    public bool CharUndeleteEnabled;
    // NYI
    public bool CommerceSystemEnabled;          // NYI
    public bool CompetitiveModeEnabled;
    public EuropaTicketConfig? EuropaTicketSystemStatus;
    // Currently active Classic season
    public List<GameRuleValuePair> GameRuleValues = new();

    public bool IsExpansionPreorderInStore;
    // NYI
    public bool KioskModeEnabled;

    public uint KioskSessionMinutes;
    public int? LaunchETA;
    public bool LiveRegionAccountCopyEnabled;
    public bool LiveRegionCharacterCopyEnabled;
    public List<int> LiveRegionCharacterCopySourceRegions = new();
    public bool LiveRegionCharacterListEnabled;
    // NYI
    // NYI
    // NYI
    public bool LiveRegionKeyBindingsCopyEnabled;

    public int MaxCharactersPerRealm;
    public int MaximumExpansionLevel;
    public short MaxPlayerNameQueriesPerPacket = 50;
    // NYI
    // NYI
    public int MinimumExpansionLevel;

    public TimeSpan PlayerNameQueryInterval = TimeSpan.FromSeconds(10);
    public short PlayerNameQueryTelemetryInterval = 600;
    public long TokenBalanceAmount;
    public bool TokenBalanceEnabled;
    public uint TokenPollTimeSeconds;
    // NYI
    // NYI
    public bool TrialBoostEnabled;

    public bool Unk14;                          // NYI
                                                // NYI
                                                // NYI
    public bool Unknown901CheckoutRelated;

    // NYI
    public bool Unused1000;

    public bool WillKickFromWorld;              // NYI
                                                // NYI
                                                // NYI 
    public FeatureSystemStatusGlueScreen() : base(ServerOpcodes.FeatureSystemStatusGlueScreen) { }

    public override void Write()
    {
        _worldPacket.WriteBit(BpayStoreEnabled);
        _worldPacket.WriteBit(BpayStoreAvailable);
        _worldPacket.WriteBit(BpayStoreDisabledByParentalControls);
        _worldPacket.WriteBit(CharUndeleteEnabled);
        _worldPacket.WriteBit(CommerceSystemEnabled);
        _worldPacket.WriteBit(Unk14);
        _worldPacket.WriteBit(WillKickFromWorld);
        _worldPacket.WriteBit(IsExpansionPreorderInStore);

        _worldPacket.WriteBit(KioskModeEnabled);
        _worldPacket.WriteBit(CompetitiveModeEnabled);
        _worldPacket.WriteBit(false); // unused, 10.0.2
        _worldPacket.WriteBit(TrialBoostEnabled);
        _worldPacket.WriteBit(TokenBalanceEnabled);
        _worldPacket.WriteBit(LiveRegionCharacterListEnabled);
        _worldPacket.WriteBit(LiveRegionCharacterCopyEnabled);
        _worldPacket.WriteBit(LiveRegionAccountCopyEnabled);

        _worldPacket.WriteBit(LiveRegionKeyBindingsCopyEnabled);
        _worldPacket.WriteBit(Unknown901CheckoutRelated);
        _worldPacket.WriteBit(false); // unused, 10.0.2
        _worldPacket.WriteBit(EuropaTicketSystemStatus.HasValue);
        _worldPacket.WriteBit(false); // unused, 10.0.2
        _worldPacket.WriteBit(LaunchETA.HasValue);
        _worldPacket.WriteBit(AddonsDisabled);
        _worldPacket.WriteBit(Unused1000);
        _worldPacket.FlushBits();

        EuropaTicketSystemStatus?.Write(_worldPacket);

        _worldPacket.WriteUInt32(TokenPollTimeSeconds);
        _worldPacket.WriteUInt32(KioskSessionMinutes);
        _worldPacket.WriteInt64(TokenBalanceAmount);
        _worldPacket.WriteInt32(MaxCharactersPerRealm);
        _worldPacket.WriteInt32(LiveRegionCharacterCopySourceRegions.Count);
        _worldPacket.WriteUInt32(BpayStoreProductDeliveryDelay);
        _worldPacket.WriteInt32(ActiveCharacterUpgradeBoostType);
        _worldPacket.WriteInt32(ActiveClassTrialBoostType);
        _worldPacket.WriteInt32(MinimumExpansionLevel);
        _worldPacket.WriteInt32(MaximumExpansionLevel);
        _worldPacket.WriteInt32(ActiveSeason);
        _worldPacket.WriteInt32(GameRuleValues.Count);
        _worldPacket.WriteInt16(MaxPlayerNameQueriesPerPacket);
        _worldPacket.WriteInt16(PlayerNameQueryTelemetryInterval);
        _worldPacket.WriteUInt32((uint)PlayerNameQueryInterval.TotalSeconds);

        if (LaunchETA.HasValue)
            _worldPacket.WriteInt32(LaunchETA.Value);

        foreach (var sourceRegion in LiveRegionCharacterCopySourceRegions)
            _worldPacket.WriteInt32(sourceRegion);

        foreach (var gameRuleValue in GameRuleValues)
            gameRuleValue.Write(_worldPacket);
    }
}