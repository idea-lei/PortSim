public static class Parameters
{

    #region Container
    public static readonly float ContainerHeight = 2.5f;
    public static readonly float ContainerWidth = 2.5f;
    public static readonly float ContainerLength_Long = 12f;
    public static readonly float ContainerLength_Short = 6f;
    public static readonly int MaxContainerWeight = 20;
    #endregion

    #region PlayGround
    public static readonly int DimZ = 3;
    public static readonly int DimX = 3;
    public static readonly int MaxLayer = 3;
    public static readonly int MinDim = 0; // this is only for test (random generation), should be smaller than the min Dim value

    public static readonly float Gap_Container = 1f;
    public static readonly float Gap_Field = 5f;
    public static readonly float SpawnInterval = 10f;

    public static readonly float PossibilityOfNewOutField = 0.001f; // this is only for test (random generation)
    #endregion

    #region Movement
    public static readonly float Vy_Loaded = 25f;
    public static readonly float Vy_Unloaded = 30f;
    public static readonly float Vx_Loaded = 25f;
    public static readonly float Vx_Unloaded = 30f;
    public static readonly float Vz_Loaded = 25f;
    public static readonly float Vz_Unloaded = 30f;

    public static readonly float TranslationHeight = 20f;
    #endregion

    #region Errors
    // this value should be larger than v_magnitude * deltatime
    public static readonly float DistanceError = 0.3f;
    public static readonly float SqrDistanceError = 0.548f; // use this to avoid the sqrt calculation
    #endregion

    #region Simulation
    public static readonly float EventDelay = 1f;
    public static readonly int MaxInFieldNumber = 6; // this is only for test (random generation)
    public static float InFieldGenerationInterval => DimZ * DimX * MaxLayer * 1f; // this is only for test (random generation)
    
    public static readonly float PossibilityOfDelay = 0.05f;
    public static float SetDelayInterval => DimZ * DimX * MaxLayer * 5f;
    public static int TrainingDim = 3;
    #endregion
}
