using Celeste.Mod.TASHelper.Utils;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod.Cil;
using TAS;
using CMCore = Celeste.Mod.Core;

namespace Celeste.Mod.TASHelper.Gameplay;

public static class ConsoleEnhancement {

    private static bool openConsole = false;

    private static bool lastOpen = false;
    public static void SetOpenConsole() {
        if (Manager.Running && !lastOpen) {
            openConsole = true;
        }
    }
    public static bool GetOpenConsole() {
        if (openConsole) {
            openConsole = false;
            return true;
        }
        return false;
    }

    [Load]
    public static void Load() {
        IL.Monocle.Commands.UpdateClosed += ILCommandUpdateClosed;
    }

    [Unload]
    public static void Unload() {
        IL.Monocle.Commands.UpdateClosed -= ILCommandUpdateClosed;
    }

    [Initialize]
    public static void Initialize() {
        typeof(Manager).GetMethod("Update").HookAfter(UpdateCommands);
    }

    private static void UpdateCommands() {
        if (Manager.Running && TasHelperSettings.EnableOpenConsoleInTas) {
            lastOpen = Engine.Commands.Open;
            if (Engine.Commands.Open) {
                Engine.Commands.UpdateOpen();
            }
            else if (Engine.Commands.Enabled) {
                Engine.Commands.UpdateClosed();
            }
        }
    }

    private static void ILCommandUpdateClosed(ILContext context) {
        ILCursor cursor = new ILCursor(context);
        if (cursor.TryGotoNext(MoveType.AfterLabel,
            ins => ins.MatchCallOrCallvirt<CMCore.CoreModule>("get_Settings"),
            ins => ins.MatchCallOrCallvirt<CMCore.CoreModuleSettings>("get_DebugConsole"),
            ins => ins.MatchCallOrCallvirt<ButtonBinding>("get_Pressed"))) {
            ILLabel target;
            if (cursor.Next.Next.Next.Next.OpCode == OpCodes.Brtrue_S) { // depends on version of Everest
                target = (ILLabel)cursor.Next.Next.Next.Next.Operand;
            }
            else if (cursor.Prev.OpCode == OpCodes.Brtrue_S){
                target = (ILLabel)cursor.Prev.Operand;
            }
            else {
                return;
            }
            cursor.EmitDelegate(GetOpenConsole);
            cursor.Emit(OpCodes.Brtrue_S, target);
        }
    }
}