using System;
using Framework.Constants;
using Game.Maps;

namespace Game.Entities;

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
		if (passenger.GetMap() != map)
			return;

		// Do not use Unit::UpdatePosition here, we don't want to remove auras
		// as if regular movement occurred
		switch (passenger.GetTypeId())
		{
			case TypeId.Unit:
			{
				var creature = passenger.ToCreature();
				map.CreatureRelocation(creature, pos, false);

				if (setHomePosition)
				{
					pos = creature.GetTransportHomePosition();
					transport.CalculatePassengerPosition(pos);
					creature.SetHomePosition(pos);
				}

				break;
			}
			case TypeId.Player:
				//relocate only passengers in world and skip any player that might be still logging in/teleporting
				if (passenger.IsInWorld && !passenger.ToPlayer().IsBeingTeleported())
				{
					map.PlayerRelocation(passenger.ToPlayer(), pos);
					passenger.ToPlayer().SetFallInformation(0, passenger.Location.Z);
				}

				break;
			case TypeId.GameObject:
				map.GameObjectRelocation(passenger.ToGameObject(), pos, false);
				passenger.ToGameObject().RelocateStationaryPosition(pos);

				break;
			case TypeId.DynamicObject:
				map.DynamicObjectRelocation(passenger.ToDynamicObject(), pos);

				break;
			case TypeId.AreaTrigger:
				map.AreaTriggerRelocation(passenger.ToAreaTrigger(), pos);

				break;
			default:
				break;
		}

		var unit = passenger.ToUnit();

		if (unit != null)
		{
			var vehicle = unit.GetVehicleKit();

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