using Celeste.Mod.TASHelper.Entities;

namespace Celeste.Mod.TASHelper.Utils;

internal static class Loader {

    public static void EntityLoad() {
        LoadRangeCountDownCameraTarget.Load();
        PixelGridHook.Load();
        SimplifiedSpinner.Load();
    }

    public static void EntityUnload() {
        LoadRangeCountDownCameraTarget.Unload();
        PixelGridHook.Unload();
        SimplifiedSpinner.Unload();
    }

    public static void HelperLoad() {
        PlayerHelper.Load();
        RenderHelper.Load();
        SpinnerHelper.Load();
        SimplifiedSpinner.Load();
        Utils.Debug.DebugHelper.Load();
    }
    public static void HelperUnload() {
        PlayerHelper.Unload();
        RenderHelper.Unload();
        SpinnerHelper.Unload();
        SimplifiedSpinner.Unload();
        HookHelper.Unload();
        Utils.Debug.DebugHelper.Unload();
    }

    public static void HelperInitialize() {
        RenderHelper.Initialize();
        SpinnerHelper.Initialize();
        ModUtils.Initialize();
    }
}