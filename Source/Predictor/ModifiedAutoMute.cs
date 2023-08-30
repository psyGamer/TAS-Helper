﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Metadata;
using Celeste;
using Celeste.Mod.TASHelper.Utils;
using FMOD;
using FMOD.Studio;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using StudioCommunication;
using TAS.Module;
using TAS.Utils;

namespace Celeste.Mod.TASHelper.Predictor;

// copied from tas mod
public static class ModifiedAutoMute {
    private static readonly HashSet<string> LoopAudioPaths = new() {
        "event:/char/madeline/wallslide",
        "event:/char/madeline/dreamblock_travel",
        "event:/char/madeline/water_move_shallow",
        "event:/char/badeline/wallslide",
        "event:/char/badeline/dreamblock_travel",
        "event:/char/badeline/water_move_shallow",
        "event:/char/badeline/boss_bullet",
        "event:/ui/game/memorial_dream_loop",
        "event:/ui/game/memorial_dream_text_loop",
        "event:/ui/game/memorial_text_loop",
        "event:/game/general/birdbaby_tweet_loop",
        "event:/game/general/crystalheart_blue_get",
        "event:/game/general/crystalheart_red_get",
        "event:/game/general/crystalheart_gold_get",
        "event:/game/00_prologue/bridge_rumble_loop",
        "event:/game/01_forsaken_city/birdbros_fly_loop",
        "event:/game/01_forsaken_city/console_static_loop",
        "event:/game/02_old_site/sequence_phone_ring_loop",
        "event:/game/02_old_site/sequence_phone_ringtone_loop",
        "event:/game/03_resort/platform_vert_down_loop",
        "event:/game/03_resort/platform_vert_up_loop",
        "event:/game/04_cliffside/arrowblock_move",
        "event:/game/04_cliffside/gondola_movement_loop",
        "event:/game/04_cliffside/gondola_halted_loop",
        "event:/game/04_cliffside/gondola_movement_loop",
        "event:/game/05_mirror_temple/mainmirror_torch_loop",
        "event:/game/05_mirror_temple/redbooster_move",
        "event:/game/05_mirror_temple/swapblock_return",
        "event:/game/06_reflection/badeline_pull_rumble_loop",
        "event:/game/06_reflection/crushblock_move_loop",
        "event:/game/06_reflection/crushblock_move_loop_covert",
        "event:/game/06_reflection/crushblock_return_loop",
        "event:/game/06_reflection/feather_state_loop",
        "event:/game/06_reflection/badeline_pull_rumble_loop",
        "event:/game/09_core/conveyor_activate",
        "event:/game/09_core/rising_threat",
        "event:/new_content/game/10_farewell/glider_movement",
        "event:/new_content/game/10_farewell/fakeheart_get",
        "event:/CommunalHelperEvents/game/connectedDreamBlock/dreamblock_fly_travel",
        "event:/CommunalHelperEvents/game/dreamSwapBlock/dream_swap_block_return",
        "event:/CommunalHelperEvents/game/redirectMoveBlock/arrowblock_move",
        "event:/CommunalHelperEvents/game/melvin/move_loop",
        "event:/CommunalHelperEvents/game/railedMoveBlock/railedmoveblock_move",
        "event:/CommunalHelperEvents/game/customBoosters/dreamBooster/dreambooster_move",
    };

    private static readonly IDictionary<WeakReference<EventInstance>, int>
        LoopAudioInstances = new ConcurrentDictionary<WeakReference<EventInstance>, int>();

    private static bool settingMusic;

    private static bool inPredict = false;
    private static bool ShouldBeMuted => inPredict && !settingMusic;
    private static WeakReference<EventInstance> dummy;

    private static EventInstance DummyEventInstance {
        get {
            if (dummy == null || !dummy.TryGetTarget(out EventInstance dummyInstance)) {
                // this sound does exist, but is silent if we don't set any audio param to it.
                bool temp = settingMusic;
                settingMusic = true;
                dummyInstance = Audio.CreateInstance("event:/char/madeline/footstep");
                settingMusic = temp;
                dummyInstance.setVolume(0);
                dummy = new WeakReference<EventInstance>(dummyInstance);
            }

            return dummyInstance;
        }
    }

    public static void Load() {
        On.Celeste.Audio.SetMusic += AudioOnSetMusic;
        On.Celeste.Audio.SetAltMusic += AudioOnSetAltMusic;
        On.FMOD.Studio.EventDescription.createInstance += EventDescriptionOnCreateInstance;
        IL.Celeste.CassetteBlockManager.AdvanceMusic += CassetteBlockManagerOnAdvanceMusic;
    }


    public static void Unload() {
        On.Celeste.Audio.SetMusic -= AudioOnSetMusic;
        On.Celeste.Audio.SetAltMusic -= AudioOnSetAltMusic;
        On.FMOD.Studio.EventDescription.createInstance -= EventDescriptionOnCreateInstance;
        IL.Celeste.CassetteBlockManager.AdvanceMusic -= CassetteBlockManagerOnAdvanceMusic;
    }

    private static void AudioOnSetAltMusic(On.Celeste.Audio.orig_SetAltMusic orig, string path) {
        settingMusic = true;
        orig(path);
        settingMusic = false;
    }

    private static bool AudioOnSetMusic(On.Celeste.Audio.orig_SetMusic orig, string path, bool startPlaying, bool allowFadeOut) {
        settingMusic = true;
        bool result = orig(path, startPlaying, allowFadeOut);
        settingMusic = false;
        return result;
    }

    private static RESULT EventDescriptionOnCreateInstance(On.FMOD.Studio.EventDescription.orig_createInstance orig, EventDescription self,
        out EventInstance instance) {
        RESULT result;
        string path = Audio.GetEventName(self);
        if (ShouldBeMuted && path.IsNotNullOrEmpty()) {
            result = RESULT.OK;
            instance = DummyEventInstance;
        }
        else {
            result = orig(self, out instance);
        }

        if (!ShouldBeMuted && instance != null && path.IsNotNullOrEmpty()) {
            int delayFrames = -1;
            if (LoopAudioPaths.Contains(path)) {
                delayFrames = 10;
            }
            else if (path.StartsWith("event:/env/local/") || path.StartsWith("event:/new_content/env/") ||
                       path.StartsWith("event:/char/dialogue/")) {
                delayFrames = 0;
            }

            if (delayFrames >= 0) {
                LoopAudioInstances.Add(new WeakReference<EventInstance>(instance), delayFrames);
            }
        }

        return result;
    }

    private static void CassetteBlockManagerOnAdvanceMusic(ILContext il) {
        ILCursor ilCursor = new(il);
        ilCursor.Goto(ilCursor.Instrs.Count - 1);
        if (ilCursor.TryGotoPrev(ins => ins.MatchLdfld<CassetteBlockManager>("leadBeats"), ins => ins.OpCode == OpCodes.Ldc_I4_0)) {
            ilCursor.Index++;
            ilCursor.EmitDelegate<Func<int, int>>(MuteBeats);
        }
    }

    private static int MuteBeats(int leadBeats) {
        return ShouldBeMuted ? 1 : leadBeats;
    }

    internal static void StartMute() {
        Audio.SfxVolume = 0f;
        inPredict = true;
    }

    internal static void EndMute() {
        Settings.Instance.ApplySFXVolume();
        inPredict = false;
    }
}