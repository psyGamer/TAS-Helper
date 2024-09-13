﻿#define Test_Mod_Compatibility
using Celeste.Mod.TASHelper.Entities;
using Celeste.Mod.TASHelper.Utils;
using Microsoft.Xna.Framework;
using Monocle;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace Celeste.Mod.TASHelper.Gameplay.AutoWatchEntity;

internal partial class HiresText : THRenderer {

    internal AutoWatchRenderer holder;
    public string content;
    private Vector2 position;

    public float X {
        get => position.X / 6f;
        set => position.X = value * 6f;
    }

    public float Y {
        get => position.Y / 6f;
        set => position.Y = value * 6f;
    }

    public Vector2 Position {
        get {
            return position / 6f;
        }
        set {
            position = value * 6f;
        }
    }

    public float scale = 1f;

    public Vector2 justify;

    public Color colorInside = Color.White;

    public Color colorOutside = Color.Black;

    public HiresText(string text, Vector2 position, AutoWatchRenderer holder) {
        content = text;
        this.position = position;
        this.holder = holder;
        justify = new Vector2(0.5f, 0.5f);
    }

    public override void Render() {
        if (DebugRendered && holder.Visible) {
            Message.RenderMessage(content, position, justify, Config.HiresFontSize * scale, Config.HiresFontStroke * scale, colorInside, colorOutside);
        }
    }

    public void Clear() {
        content = "";
    }

    public void Append(string s) {
        if (s == "") {
            return;
        }
        if (content == "") {
            content = s;
        }
        else {
            content += "\n" + s;
        }
    }

    public void AppendAtFirst(string shortTitle, string longTitle) {
        if (FindLines.Count(content) > 0) {
            content = longTitle + "\n" + content;
        }
        else {
            content = shortTitle + ": " + content;
        }
    }

    public void Newline(int lines = 1) {
        content += new string('\n', lines);
    }

    private static readonly Regex FindLines = new Regex("\n", RegexOptions.Compiled);
}

internal class AutoWatchTextRenderer : AutoWatchRenderer {

    public HiresText text;
    public AutoWatchTextRenderer(RenderMode mode, bool active, bool preActive = false) : base(mode, active, preActive) { }

    public override void Added(Entity entity) {
        base.Added(entity);
        HiresLevelRenderer.Add(text = new HiresText("", entity.Position, this));
    }

    public override void EntityAdded(Scene scene) {
        base.EntityAdded(scene);
        if (text is not null) {
            HiresLevelRenderer.AddIfNotPresent(text); // without this, PlayerRender may get lost after EventTrigger "ch9_goto_the_future" (first two sides of Farewell)
        }
    }

    public override void Removed(Entity entity) {
        base.Removed(entity);
        if (text is not null) {
            HiresLevelRenderer.Remove(text);
        }
    }
    public override void EntityRemoved(Scene scene) {
        base.EntityRemoved(scene);
        if (text is not null) {
            HiresLevelRenderer.Remove(text);
        }
    }

    public void SetVisible() {
        Visible = text.content != "";
    }

    public override void OnClone() {
        base.OnClone();
        if (text is not null) {
            HiresLevelRenderer.AddIfNotPresent(text);
        }
    }
}

internal class AutoWatchText2Renderer : AutoWatchTextRenderer {
    public HiresText textBelow;

    public Vector2 offset = Vector2.UnitY * 6f;
    public AutoWatchText2Renderer(RenderMode mode, bool active, bool preActive = false) : base(mode, active, preActive) { }

    public override void Added(Entity entity) {
        base.Added(entity);
        HiresLevelRenderer.Add(textBelow = new HiresText("", entity.Position, this));
        textBelow.justify = new Vector2(0.5f, 0f);
    }

    public override void EntityAdded(Scene scene) {
        base.EntityAdded(scene);
        if (textBelow is not null) {
            HiresLevelRenderer.AddIfNotPresent(textBelow);
        }
    }

    public override void Removed(Entity entity) {
        base.Removed(entity);
        if (textBelow is not null) {
            HiresLevelRenderer.Remove(textBelow);
        }
    }
    public override void EntityRemoved(Scene scene) {
        base.EntityRemoved(scene);
        if (textBelow is not null) {
            HiresLevelRenderer.Remove(textBelow);
        }
    }

    public override void OnClone() {
        base.OnClone();
        if (textBelow is not null) {
            HiresLevelRenderer.AddIfNotPresent(textBelow);
        }
    }

    public new void SetVisible() {
        Visible = (text.content != "" || textBelow.content != "");
    }
}

internal static class InfoParser {

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string SignedString(this float f) {
        return f.ToString("+0.00;-0.00;0.00");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string IntSignedString(this float f) {
        return f.ToString("+0;-0;0");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int ToCeilingFrames(float seconds) {
        return (int)Math.Ceiling(seconds / Engine.DeltaTime);
    }


    internal static int ToFrameData(this float seconds) {
        return ToCeilingFrames(seconds);
    }
    internal static string ToFrame(this float seconds) {
        if (seconds <= 0) {
            return "";
        }
        return ToCeilingFrames(seconds).ToString();
    }
    internal static string ToFrameAllowZero(this float seconds) {
        return ToCeilingFrames(seconds).ToString();
    }

    internal static string ToFrameMinusOne(this float seconds) {
        if (seconds <= 0) {
            return "";
        }
        return (ToCeilingFrames(seconds) - 1).ToString();
    }

    internal static string ToFrame(this int frames) {
        return frames.ToString();
    }

    internal static string PositionToAbsoluteSpeed(this Vector2 vector) {
        if (Format.Speed_UsePixelPerSecond) {
            return (vector.Length() / Engine.DeltaTime).ToString("0.00");
        }
        else { // pixel per frame
            return vector.Length().ToString("0.00");
        }
    }

    internal static string Speed2ToSpeed2(this Vector2 speed) {
        if (IsTiny(speed.X)) {
            if (IsTiny(speed.Y)) {
                return "";
            }
            return speed.Y.SpeedToSpeed();
        }
        else if (IsTiny(speed.Y)) {
            return speed.X.SpeedToSpeed();
        }
        return $"({speed.X.SpeedToSpeed()}, {speed.Y.SpeedToSpeed()})";
    }

    internal static string Speed2ToSpeed2ButBreakline(this Vector2 speed) {
        if (IsTiny(speed.X)) {
            if (IsTiny(speed.Y)) {
                return "";
            }
            return speed.Y.SpeedToSpeed();
        }
        else if (IsTiny(speed.Y)) {
            return speed.X.SpeedToSpeed();
        }
        return $"({speed.X.SpeedToSpeed()},\n {speed.Y.SpeedToSpeed()})";
    }

    internal static string SpeedToSpeed(this float speed) { // in case we do have a speed field
        if (Format.Speed_UsePixelPerSecond) {
            return speed.SignedString();
        }
        else { // pixel per frame
            return (speed * Engine.DeltaTime).SignedString();
        }
    }

    private static string PositionToSignedSpeedX(this float f) {
        if (Format.Speed_UsePixelPerSecond) {
            return (f / Engine.DeltaTime).SignedString();
        }
        else {
            return f.SignedString();
        }
    }

    internal static string Positon2ToSignedSpeed(this Vector2 deltaPosition, bool allowZero = false, bool breakline = false) {
        if (!allowZero) {
            if (IsTiny(deltaPosition.X)) {
                if (IsTiny(deltaPosition.Y)) {
                    return "";
                }
                return PositionToSignedSpeedX(deltaPosition.Y);
            }
            else if (IsTiny(deltaPosition.Y)) {
                return PositionToSignedSpeedX(deltaPosition.X);
            }
        }

        if (breakline) {
            return $"X: {PositionToSignedSpeedX(deltaPosition.X)}\nY: {PositionToSignedSpeedX(deltaPosition.Y)}";
        }
        else {
            return $"({PositionToSignedSpeedX(deltaPosition.X)}, {PositionToSignedSpeedX(deltaPosition.Y)})";
        }
    }

    internal static string OffsetToString(this Vector2 deltaPosition, bool allowZero = false, bool breakline = false) {
        if (!allowZero) {
            if (IsTiny(deltaPosition.X)) {
                if (IsTiny(deltaPosition.Y)) {
                    return "";
                }
                return SignedString(deltaPosition.Y);
            }
            else if (IsTiny(deltaPosition.Y)) {
                return SignedString(deltaPosition.X);
            }
        }
        if (breakline) {
            return $"X: {SignedString(deltaPosition.X)}\nY: {SignedString(deltaPosition.Y)}";
        }
        else {
            return $"({SignedString(deltaPosition.X)}, {SignedString(deltaPosition.Y)})";
        }
    }

    internal static string FloatVector2ToString(this Vector2 vector2) {
        return $"({SignedString(vector2.X)}, {SignedString(vector2.Y)})";
    }
    internal static string IntVector2ToString(this Vector2 vector2) {
        return $"({IntSignedString(vector2.X)}, {IntSignedString(vector2.Y)})";
    }

    internal static string AbsoluteFloatToString(this float f) {
        return f.ToString("0.00");
    }
    internal static string SignedFloatToString(this float f) {
        return SignedString(f);
    }

    internal static string SignedIntToString(this float f) {
        return IntSignedString(f);
    }

    internal const float epsilon = 1E-6f;

    internal const float Minus_epsilon = -1E-6f;

    private static bool IsTiny(float f) {
        return f < epsilon && f > Minus_epsilon;
    }
}

internal static class CoroutineFinder {

    // note that if it's hooked, then the name will change
    // like Celeste.FallingBlock+<Sequence>d__21 -> HonlyHelper.RisingBlock+<FallingBlock_Sequence>d__5
    // even if the block itself is a FallingBlock instead of a RisingBlock
    public static bool FindCoroutineComponent(this Entity entity, string compiler_generated_class_name, out Tuple<Coroutine, System.Collections.IEnumerator> pair) {
        // e.g. compiler_generated_class_name = Celeste.FallingBlock+<Sequence>d__21

#if Test_Mod_Compatibility
        foreach (Component c in entity.Components) {
            if (c is not Coroutine coroutine) {
                continue;
            }
            if (coroutine.enumerators.FirstOrDefault(functioncall => functioncall.GetType().FullName == compiler_generated_class_name) is System.Collections.IEnumerator func) {
                pair = Tuple.Create(coroutine, func);
                return true;
            }
            //Logger.Log(LogLevel.Debug, "TASHelper", string.Join(" + ", coroutine.enumerators.Select(functionCall => functionCall.GetType().FullName)));
        }
        // throw new Exception($"AutoWatchEntity: can't find {compiler_generated_class_name} of {entity.GetEntityId()}");
        Logger.Log(LogLevel.Error, "TASHelper", $"AutoWatchEntity: can't find {compiler_generated_class_name} of {entity.GetEntityId()}");
        pair = null;
        return false;

#else
        foreach (Component c in entity.Components) {
            if (c is not Coroutine coroutine) {
                continue;
            }
            if (coroutine.enumerators.FirstOrDefault(functioncall => functioncall.GetType().FullName == compiler_generated_class_name) is System.Collections.IEnumerator func) {
                pair = Tuple.Create(coroutine, func);
                return true;
            }
        }
        pair = null;
        return false;
#endif
    }

    public static System.Collections.IEnumerator FindIEnumrator(this Coroutine coroutine, string compiler_generated_class_name) {
        if (coroutine.enumerators.FirstOrDefault(functioncall => functioncall.GetType().Name == compiler_generated_class_name) is System.Collections.IEnumerator func) {
            return func;
        }
        return null;
    }
}




