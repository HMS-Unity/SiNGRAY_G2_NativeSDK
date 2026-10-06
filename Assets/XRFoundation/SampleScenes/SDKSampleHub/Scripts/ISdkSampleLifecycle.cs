namespace Singray.Foundation.SampleScenes
{
    /// <summary>
    /// Implemented by sample modules that own native sensors or frame callbacks.
    /// The hub invokes this before disabling and destroying the active module.
    /// </summary>
    public interface ISdkSampleLifecycle
    {
        void ShutdownSample();
    }
}
