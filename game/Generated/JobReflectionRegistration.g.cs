// Register the project texture job before the first scene loads.
namespace YuGiOhFight.Client.Presentation
{
    public static partial class TextureProcessor
    {
        internal static void InitializeOpaqueCheckJob() =>
            Unity.Jobs.IJobParallelForExtensions.EarlyJobInit<OpaqueCheckJob>();
    }

    [Unity.Jobs.DOTSCompilerGenerated]
    internal static class ClientJobRegistration
    {
        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        public static void EarlyInit()
        {
            try { TextureProcessor.InitializeOpaqueCheckJob(); }
            catch (System.Exception error) { Unity.Jobs.EarlyInitHelpers.JobReflectionDataCreationFailed(error); }
        }
    }
}
