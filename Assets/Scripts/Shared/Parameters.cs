public static class Parameters
{
    #region PlayGround
    // the ground stack max layer
    public static readonly int MaxLayer = 5;

    // number of containers in z/x
    public static readonly int DimZ = 6;
    public static readonly int DimX = 8;
    public static readonly int MinDim = 0; // this is only for test (random generation), should be smaller than the min Dim value

    public static readonly float ContainerHeight = 2.5f;
    public static readonly float ContainerWidth = 2.5f;
    public static readonly float ContainerLength_Long = 12f;
    public static readonly float ContainerLength_Short = 6f;

    public static readonly float Gap_Container = 1f;
    public static readonly float Gap_Field = 10f;
    public static readonly float SpawnInterval = 10f;

    public static readonly float PossibilityOfNewOutField = 0.001f; // this is only for test (random generation)
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
    public static readonly float DistanceError = 0.05f;
    public static readonly float SqrDistanceError = 0.22f; // use this to avoid the sqrt calculation
    #endregion

    #region Simulation
    public static readonly float EventDelay = 1f;
    public static readonly int InFieldNumber = 6; // this is only for test (random generation)
    #endregion
}
