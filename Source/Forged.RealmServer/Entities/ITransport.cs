// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;
using Game.Common.Maps;

namespace Forged.RealmServer.Entities;

public interface ITransport
{
	ObjectGuid GetTransportGUID();

	// This method transforms supplied transport offsets into global coordinates
	void CalculatePassengerPosition(Position pos);

	// This method transforms supplied global coordinates into local offsets
	void CalculatePassengerOffset(Position pos);

	float GetTransportOrientation();

	void AddPassenger(WorldObject passenger);

	ITransport RemovePassenger(WorldObject passenger);

	public static void UpdatePassengerPosition(ITransport transport, Map map, WorldObject passenger, Position pos, bool setHomePosition)
	{
		// transport teleported but passenger not yet (can happen for players)
		if (passenger.Map != map)
			return;

		// Do not use Unit::UpdatePosition here, we don't want to remove auras
		// as if regular movement occurred
		switch (passenger.TypeId)
		{
			case TypeId.Unit:
			{
				var creature = passenger.AsCreature;
				map.CreatureRelocation(creature, pos, false);

				if (setHomePosition)
				{
					pos = creature.TransportHomePosition;
					transport.CalculatePassengerPosition(pos);
					creature.HomePosition = pos;
				}

				break;
			}
			case TypeId.Player:
				//relocate only passengers in world and skip any player that might be still logging in/teleporting
				if (passenger.IsInWorld && !passenger.AsPlayer.IsBeingTeleported)
				{
					map.PlayerRelocation(passenger.AsPlayer, pos);
					passenger.AsPlayer.SetFallInformation(0, passenger.Location.Z);
				}

				break;
			case TypeId.GameObject:
				map.GameObjectRelocation(passenger.AsGameObject, pos, false);
				passenger.AsGameObject.RelocateStationaryPosition(pos);

				break;
			case TypeId.DynamicObject:
				map.DynamicObjectRelocation(passenger.AsDynamicObject, pos);

				break;
			case TypeId.AreaTrigger:
				map.AreaTriggerRelocation(passenger.AsAreaTrigger, pos);

				break;
			default:
				break;
		}

		var unit = passenger.AsUnit;

		if (unit != null)
		{
			var vehicle = unit.VehicleKit1;

			if (vehicle != null)
				vehicle.RelocatePassengers();
		}
	}

	static void CalculatePassengerPosition(Position pos, float transX, float transY, float transZ, float transO)
	{
		float inx = pos.X, iny = pos.Y, inz = pos.Z;
		pos.Orientation = Position.NormalizeOrientation(transO + pos.Orientation);

		pos.X = transX + inx * MathF.Cos(transO) - iny * MathF.Sin(transO);
		pos.Y = transY + iny * MathF.Cos(transO) + inx * MathF.Sin(transO);
		pos.Z = transZ + inz;
	}

	static void CalculatePassengerOffset(Position pos, float transX, float transY, float transZ, float transO)
	{
		pos.Orientation = Position.NormalizeOrientation(pos.Orientation - transO);

		pos.Z -= transZ;
		pos.Y -= transY; // y = searchedY * std::cos(o) + searchedX * std::sin(o)
		pos.X -= transX; // x = searchedX * std::cos(o) + searchedY * std::sin(o + pi)
		float inx = pos.X, iny = pos.Y;
		pos.Y = (iny - inx * MathF.Tan(transO)) / (MathF.Cos(transO) + MathF.Sin(transO) * MathF.Tan(transO));
		pos.X = (inx + iny * MathF.Tan(transO)) / (MathF.Cos(transO) + MathF.Sin(transO) * MathF.Tan(transO));
	}

	int GetMapIdForSpawning();
}