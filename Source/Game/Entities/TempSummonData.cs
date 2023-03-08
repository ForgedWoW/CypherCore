using Framework.Constants;

namespace Game.Entities;

public class TempSummonData
{
	public uint entry;          // Entry of summoned creature
	public Position pos;        // Position, where should be creature spawned
	public TempSummonType type; // Summon type, see TempSummonType for available types
	public uint time;           // Despawn time, usable only with certain temp summon types
}