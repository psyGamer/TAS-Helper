using Celeste.Mod.Helpers;
using MonoMod.Utils;
using System.Reflection;

namespace Celeste.Mod.TASHelper.Utils;

// completely taken from Celeste TAS
internal static class ModUtils {
    public static readonly Assembly VanillaAssembly = typeof(Player).Assembly;

#pragma warning disable CS8603
    public static Type GetType(string modName, string name, bool throwOnError = false, bool ignoreCase = false) {
        return GetAssembly(modName)?.GetType(name, throwOnError, ignoreCase);
    }

    public static Type GetType(string name, bool throwOnError = false, bool ignoreCase = false) {
        return FakeAssembly.GetFakeEntryAssembly().GetType(name, throwOnError, ignoreCase);
    }

    public static Type[] GetTypes() {
        return FakeAssembly.GetFakeEntryAssembly().GetTypes();
    }

    public static EverestModule GetModule(string modName) {
        return Everest.Modules.FirstOrDefault(module => module.Metadata?.Name == modName);
    }

    public static bool IsInstalled(string modName) {
        return GetModule(modName) != null;
    }

    public static Assembly GetAssembly(string modName) {
        return GetModule(modName)?.GetType().Assembly;
    }
#pragma warning restore CS8603

    public static bool FrostHelperInstalled = false;

    public static bool VivHelperInstalled = false;

    public static bool PandorasBoxInstalled = false;

    public static bool ExtendedVariantInstalled = false;

    public static bool ChronoHelperInstalled = false;

    public static bool BrokemiaHelperInstalled = false;

    public static bool IsaGrabBagInstalled = false;

    public static bool SpeedrunToolInstalled = false;
    public static bool UpsideDown => ExtendedVariantsUtils.UpsideDown;
    public static void InitializeAtFirst() {
        FrostHelperInstalled = IsInstalled("FrostHelper");
        VivHelperInstalled = IsInstalled("VivHelper");
        PandorasBoxInstalled = IsInstalled("PandorasBox");
        ExtendedVariantInstalled = IsInstalled("ExtendedVariantMode");
        ChronoHelperInstalled = IsInstalled("ChronoHelper");
        BrokemiaHelperInstalled = IsInstalled("BrokemiaHelper");
        IsaGrabBagInstalled = IsInstalled("IsaGrabBag");
        SpeedrunToolInstalled = IsInstalled("SpeedrunTool");
        // we actually also assume they are in enough late version
        // so all entities mentioned in corresponding hooks do exist
    }


    private static class ExtendedVariantsUtils {
        private static readonly Lazy<EverestModule> module = new(() => ModUtils.GetModule("ExtendedVariantMode"));
        private static readonly Lazy<object> triggerManager = new(() => module.Value?.GetFieldValue<object>("TriggerManager"));

        private static readonly Lazy<FastReflectionDelegate> getCurrentVariantValue = new(() =>
            triggerManager.Value?.GetType().GetMethodInfo("GetCurrentVariantValue")?.GetFastDelegate());

        private static readonly Lazy<Type> variantType =
            new(() => module.Value?.GetType().Assembly.GetType("ExtendedVariants.Module.ExtendedVariantsModule+Variant"));

        // enum value might be different between different ExtendedVariantMode version, so we have to parse from string
        private static readonly Lazy<object> upsideDownVariant = new(ParseVariant("UpsideDown"));

        public static Func<object> ParseVariant(string value) {
            return () => {
                try {
                    return variantType.Value == null ? null : Enum.Parse(variantType.Value, value);
                }
                catch (Exception e) {
                    return null;
                }
            };
        }

        public static bool UpsideDown => GetCurrentVariantValue(upsideDownVariant) is { } value && (bool)value;

        public static object GetCurrentVariantValue(Lazy<object> variant) {
            if (variant.Value is null) return null;
            return getCurrentVariantValue.Value?.Invoke(triggerManager.Value, variant.Value);
        }
    }
}