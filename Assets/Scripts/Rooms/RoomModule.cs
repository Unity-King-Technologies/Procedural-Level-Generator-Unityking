using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewRoomModule", menuName = "WFC/Room Module")]
public class RoomModule : ScriptableObject
{
    public GameObject prefab;
    public Vector3Int dimensions = new Vector3Int(1, 1, 1);

    [System.Serializable]
    public class Socket
    {
        public string id;
        public Direction direction;
        public SocketType type;
        public int weight = 1;
    }

    public enum Direction { North, East, South, West, Up, Down }
    public enum SocketType { Entrance, Exit, Connector, Special }

    public Socket[] sockets;
    public float baseWeight = 1f;
    public int[] difficultyRange = new int[] { 1, 10 };
    public string[] tags;

    public bool ConnectsTo(RoomModule other, Direction dir)
    {
        // Socket matching logic
        foreach (var mySocket in sockets)
        {
            if (mySocket.direction == dir)
            {
                foreach (var otherSocket in other.sockets)
                {
                    Direction oppositeDir = GetOppositeDirection(dir);
                    if (otherSocket.direction == oppositeDir &&
                        mySocket.type == otherSocket.type &&
                        mySocket.id == otherSocket.id)
                    {
                        return true;
                    }
                }
            }
        }
        return false;
    }

    private Direction GetOppositeDirection(Direction dir)
    {
        switch (dir)
        {
            case Direction.North: return Direction.South;
            case Direction.South: return Direction.North;
            case Direction.East: return Direction.West;
            case Direction.West: return Direction.East;
            case Direction.Up: return Direction.Down;
            case Direction.Down: return Direction.Up;
            default: return dir;
        }
    }
}
