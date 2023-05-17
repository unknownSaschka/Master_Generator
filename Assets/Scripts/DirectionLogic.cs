using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DirectionLogic : MonoBehaviour
{
    public enum Direction { North, East, South, West }

    public static Direction GetRotation(Direction parent, Direction child) 
    { 
        return (Direction)(((int)parent + (int)child) % 4);
    }

    public static int GetRotationAmount(Direction before, Direction after)
    {
        int multiplier = ((int)after - (int)before) % 4;
        return 90 * multiplier;
    }

    public static Direction GetDirection(Transform gameObject)
    {
        int roation = (int) gameObject.eulerAngles.y;
        return (Direction)((roation / 90) % 4);
    }

    public static int GetRoation(Direction direction)
    {
        return (int)direction * 90;
    }
}
