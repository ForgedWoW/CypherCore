namespace Forged.MapServer.Events;

public class GameEventFinishCondition
{
    public float Done;
    public uint DoneWorldState;
    // done number
    public uint MaxWorldState;

    public float ReqNum; // required number // use float, since some events use percent
    // max resource count world state update id
    // done resource count world state update id
}