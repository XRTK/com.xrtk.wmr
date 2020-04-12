using XRTK.Definitions.CameraSystem;

#if WINDOWS_UWP
using Windows.Perception;
using Windows.UI.Input.Spatial;
#endif

namespace XRTK.Providers.CameraSystem
{
    public class WindowsMixedRealityCameraDataProvider : BaseCameraDataProvider
    {
        public WindowsMixedRealityCameraDataProvider(string name, uint priority, BaseMixedRealityCameraDataProviderProfile profile)
            : base(name, priority, profile)
        {
        }

        public override void Initialize()
        {
#if WINDOWS_UWP
#endif
        }
    }
}