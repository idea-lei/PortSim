using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Parameters
{
    #region PlayGround
    // the ground stack max layer
    public static readonly int MaxLayer = 3;

    // number of containers in z/x
    public static readonly int DimZ = 3;
    public static readonly int DimX = 3;
    public static readonly int MinDim = 0; // this is only for test (random generation)

    public static readonly float ContainerHeight = 2.5f;
    public static readonly float ContainerWidth = 2.5f;
    public static readonly float ContainerLength_Long = 12f;
    public static readonly float ContainerLength_Short = 6f;

    public static readonly float Gap_Container = 1f;
    public static readonly float Gap_Field = 5f;
    public static readonly float SpawnInterval = 10f;

    public static readonly float PossibilityOfNewOutField = 0.1f; // this is only for test (random generation)
    #endregion

    #region Movement
    // the speed of the hook in different direction
    public static readonly float Vy_Loaded = 15f;
    public static readonly float Vy_Unloaded = 20f;
    public static readonly float Vx_Loaded = 15f;
    public static readonly float Vx_Unloaded = 20f;
    public static readonly float Vz_Loaded = 15f;
    public static readonly float Vz_Unloaded = 20f;

    // height of the horizontal translation of the hook
    public static readonly float TranslationHeight = 20f;
    #endregion

    #region Errors
    // this value should be larger than v_magnitude * deltatime
    public static readonly float DistanceError = 0.1f;
    #endregion

    #region Simulation
    public static readonly float EventDelay = 1f;
    #endregion
}
