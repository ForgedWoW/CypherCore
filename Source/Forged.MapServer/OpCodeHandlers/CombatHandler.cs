// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Units;
using Forged.MapServer.Globals;
using Forged.MapServer.Networking;
using Forged.MapServer.Networking.Packets.Combat;
using Forged.MapServer.Server;
using Framework.Constants;
using Game.Common.Handlers;
using Serilog;

// ReSharper disable UnusedMember.Local

namespace Forged.MapServer.OpCodeHandlers;

public class CombatHandler : IWorldSessionHandler
{
    private readonly ObjectAccessor _objectAccessor;
    private readonly WorldSession _session;

    public CombatHandler(WorldSession session, ObjectAccessor objectAccessor)
    {
        _session = session;
        _objectAccessor = objectAccessor;
    }

    [WorldPacketHandler(ClientOpcodes.AttackStop, Processing = PacketProcessing.Inplace)]
    private void HandleAttackStop(AttackStop packet)
    {
        if (packet != null)
            _session.Player.AttackStop();
    }

    [WorldPacketHandler(ClientOpcodes.AttackSwing, Processing = PacketProcessing.Inplace)]
    private void HandleAttackSwing(AttackSwing packet)
    {
        var enemy = _objectAccessor.GetUnit(_session.Player, packet.Victim);

        if (enemy == null)
        {
            // stop attack state at client
            SendAttackStop(null);

            return;
        }

        if (!_session.Player.WorldObjectCombat.IsValidAttackTarget(enemy))
        {
            // stop attack state at client
            SendAttackStop(enemy);

            return;
        }

        //! Client explicitly checks the following before sending CMSG_ATTACKSWING packet,
        //! so we'll place the same check here. Note that it might be possible to reuse this snippet
        //! in other places as well.
        if (_session.Player.Vehicle != null)
        {
            var seat = _session.Player.Vehicle.GetSeatForPassenger(_session.Player);

            if (!seat.HasFlag(VehicleSeatFlags.CanAttack))
            {
                SendAttackStop(enemy);

                return;
            }
        }

        _session.Player.Attack(enemy, true);
    }

    [WorldPacketHandler(ClientOpcodes.SetSheathed, Processing = PacketProcessing.Inplace)]
    private void HandleSetSheathed(SetSheathed packet)
    {
        if (packet.CurrentSheathState >= (int)SheathState.Max)
        {
            Log.Logger.Error("Unknown sheath state {0} ??", packet.CurrentSheathState);

            return;
        }

        _session.Player.Sheath = (SheathState)packet.CurrentSheathState;
    }

    private void SendAttackStop(Unit enemy)
    {
        _session.SendPacket(new SAttackStop(_session.Player, enemy));
    }
}