﻿using Celeste.Mod.TASHelper.Entities;
using Celeste.Mod.TASHelper.Utils;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.TASHelper.Gameplay.Spinner;

public static class ExactSpinnerGroup {

    public static bool Enabled = false;

    // hazard with offset group in (LevelGroup, LevelGroup + 1] will activate this frame (when TimeRate = 1)
    public static double ExactLevelGroup => ((double)Engine.Scene.TimeActive * 60 - 1 + GroupPeriod) % GroupPeriod;

    internal static Dictionary<Entity, Tuple<bool, string>> offsetGroup = new();

    public static int GroupPeriod = -1; // initial state need to be different from 3, 15 in order to activate later

    public static double ExactHazardGroup(Entity entity) {
        if (SpinnerCalculateHelper.HazardType(entity) is not null) {
            return (double)SpinnerCalculateHelper.GetOffset(entity)! * 60 % GroupPeriod;
        }
        return -99;
    }

    public static void SetModulo(int modulo) {
        if (Enabled && modulo < 0) {
            UnloadExactSpinnerGroup();
            Enabled = false;
        }
        else if (!Enabled && modulo > 0) {
            Enable();
            Enabled = true;
        }
        else if (Enabled && modulo > 0) {
            if (modulo != GroupPeriod) {
                LoadExactSpinnerGroup();
            }
        }
        GroupPeriod = modulo;
        if (ExactLevelGroupRenderer.Instance is { } obj) {
            obj.Visible = Enabled;
            obj.text = $"SpinnerGroup ?/{GroupPeriod}";
        }
    }

    public static void Enable() {
        if (Engine.Scene is Level) {
            LoadExactSpinnerGroup();
            LoadExactLevelGroup();
        }
    }

    public static void UnloadExactSpinnerGroup() {
        ExactSpinnerGroupRenderer.Instance?.Apply(x => { HiresLevelRenderer.Remove(x); });
    }

    [Load]
    public static void Load() {
        On.Celeste.Level.LoadLevel += OnLoadLevel;
    }

    [Unload]
    public static void Unload() {
        On.Celeste.Level.LoadLevel -= OnLoadLevel;
    }

    private static void OnLoadLevel(On.Celeste.Level.orig_LoadLevel orig, Level level, Player.IntroTypes playerIntro, bool isFromLoader = false) {
        orig(level, playerIntro, isFromLoader);
        if (Enabled) {
            LoadExactSpinnerGroup();
            LoadExactLevelGroup();
        }
    }

    public static void LoadExactSpinnerGroup() {
        if (ExactSpinnerGroupRenderer.Instance is null || !HiresLevelRenderer.Contains(ExactSpinnerGroupRenderer.Instance)) {
            HiresLevelRenderer.Add(new ExactSpinnerGroupRenderer());
        }
        else {
            ExactSpinnerGroupRenderer.Instance.Active = true;
        }
    }

    public static void LoadExactLevelGroup() {
        ExactLevelGroupRenderer.AddIfNecessary();
    }

    private class ExactLevelGroupRenderer : Message {

        internal static ExactLevelGroupRenderer Instance;

        public ExactLevelGroupRenderer() : base($"SpinnerGroup ?/{GroupPeriod}", new Vector2(1900f, 20f)) {
            this.Depth = -20000;
            this.Visible = true;
            this.Active = true;
            base.Tag |= Tags.Global;
        }

        public static bool AddIfNecessary() {
            if (Engine.Scene is not Level level) {
                return false;
            }
            if (Instance is null || !level.Entities.Contains(Instance)) {
                Instance = new();
                level.AddImmediately(Instance);
            }
            return true;
        }

        public int ShowTimeRateTimer = 6;
        public override void Update() {
            if (GroupPeriod > 10) {
                text = $"SpinnerGroup {ExactLevelGroup,8:0.00000}/{GroupPeriod}";
            }
            else {
                text = $"SpinnerGroup {ExactLevelGroup:0.00000}/{GroupPeriod}";
            }
            if (Engine.TimeRate != 1f) {
                ShowTimeRateTimer = 6;
            }
            else {
                ShowTimeRateTimer--;
            }
            if (ShowTimeRateTimer > 0) {
                text += $"\nTimeRate {Engine.TimeRate:0.00}";
            }
        }

        public override void Render() {
            if (!Countdown.NotCountdownBoost) {
                return;
            }
            Message.RenderMessageJetBrainsMono(text, Position, Vector2.UnitX, Vector2.One, 1f, Color.White, Color.Black);
        }
    }

    private class ExactSpinnerGroupRenderer : THRenderer {
        public static ExactSpinnerGroupRenderer Instance;

        public bool Active;
        public ExactSpinnerGroupRenderer() {
            Instance = this;
            Active = true;
        }

        public override void Render() {
            if (!Active) {
                if (!Countdown.NotCountdownBoost) {
                    return;
                }
                Vector2 scale = new Vector2(TasHelperSettings.HiresFontSize / 10f) * 0.7f;
                Vector2 spinnerOffset = TasHelperSettings.UsingLoadRange ? loadrangeOffset : noloadrangeOffset;
                foreach (KeyValuePair<Entity, Tuple<bool, string>> pair in offsetGroup) {
                    if (pair.Value.Item1) {
                        Message.RenderMessage(pair.Value.Item2, pair.Key.Center * 6f, scale);
                    }
                    else {
                        Message.RenderMessage(pair.Value.Item2, pair.Key.Position * 6f + spinnerOffset, scale);
                    }
                }
            }
        }

        private static Vector2 loadrangeOffset = new Vector2(0f, 4f) * 6f;

        private static Vector2 noloadrangeOffset = new Vector2(0f, 0f) * 6f;
        public override void Update() {
            if (Active && Engine.Scene is Level level && !level.Transitioning) {
                offsetGroup.Clear();
                foreach (Entity entity in level.Entities) {
                    double value = ExactHazardGroup(entity);
                    if (value > -1) {
                        offsetGroup.Add(entity, new Tuple<bool, string>(entity.isLightning(), value.ToString("0.00")));
                    }
                }
                Active = false;
            }
        }
    }
}
