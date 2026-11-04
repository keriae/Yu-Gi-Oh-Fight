// Recreated from Unity-generated IL in publisher MDPro3 1.4.7-2.
public static partial class TextureProcessor
{
    internal static void InitializeGeneratedOpaqueCheckJob() => Unity.Jobs.IJobParallelForExtensions.EarlyJobInit<OpaqueCheckJob>();
}
[Unity.Jobs.DOTSCompilerGenerated]
internal class __JobReflectionRegistrationOutput__1221673671587648887
{
    public static void CreateJobReflectionData()
    {
        try { TextureProcessor.InitializeGeneratedOpaqueCheckJob(); }
        catch (System.Exception error) { Unity.Jobs.EarlyInitHelpers.JobReflectionDataCreationFailed(error); }
    }
    [UnityEngine.RuntimeInitializeOnLoadMethod((UnityEngine.RuntimeInitializeLoadType)2)]
    public static void EarlyInit() => CreateJobReflectionData();
}
