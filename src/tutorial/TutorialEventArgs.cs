using System;
using Godot;

/// <summary>
///   Base type for tutorial event arguments
/// </summary>
public class TutorialEventArgs : EventArgs
{
}

public class MicrobeEventArgs : TutorialEventArgs
{
    public MicrobeEventArgs(Microbe microbe)
    {
        Microbe = microbe;
    }

    public Microbe Microbe { get; }
}

public class RotationEventArgs : TutorialEventArgs
{
    public RotationEventArgs(Basis rotation, Vector3 rotationInDegrees)
    {
        Rotation = rotation;
        RotationInDegrees = rotationInDegrees;
    }

    /// <summary>
    ///   Quaternion of the rotation
    /// </summary>
    public Basis Rotation { get; }

    /// <summary>
    ///   Axis-wise degree rotations
    /// </summary>
    public Vector3 RotationInDegrees { get; }
}
