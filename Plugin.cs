using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;

namespace NoCreatureDust
{
    [BepInPlugin(ModGUID, ModName, ModVersion)]
    public class NoCreatureDustPlugin : BaseUnityPlugin
    {
        internal const string ModName = "NoCreatureDust";
        internal const string ModVersion = "1.0.3";
        internal const string Author = "Azumatt";
        private const string ModGUID = $"{Author}.{ModName}";
        private readonly Harmony _harmony = new(ModGUID);
        public static readonly ManualLogSource NoCreatureDustLogger = BepInEx.Logging.Logger.CreateLogSource(ModName);

        public enum Toggle
        {
            On = 1,
            Off = 0,
        }

        public void Awake()
        {
            RemoveAllVFX = config("1 - General", "Remove All Effects", Toggle.Off, "Removes all visual effects from when a creature dies, not just the vanilla dust.");
            RemoveAllRagdollVFX = config("1 - General", "Remove All Ragdoll Effects", Toggle.Off, "Removes all ragdoll effects from creatures, not just the vanilla dust. Just in case mods add more you want to remove.");
            Assembly assembly = Assembly.GetExecutingAssembly();
            _harmony.PatchAll(assembly);
        }

        private void OnDestroy()
        {
            Config.Save();
        }

        #region ConfigOptions

        public static ConfigEntry<Toggle> RemoveAllVFX = null!;
        public static ConfigEntry<Toggle> RemoveAllRagdollVFX = null!;

        private ConfigEntry<T> config<T>(string group, string name, T value, ConfigDescription description)
        {
            ConfigEntry<T> configEntry = Config.Bind(group, name, value, description);

            return configEntry;
        }

        private ConfigEntry<T> config<T>(string group, string name, T value, string description)
        {
            return config(group, name, value, new ConfigDescription(description));
        }

        private class ConfigurationManagerAttributes
        {
            [UsedImplicitly] public int? Order = null!;
            [UsedImplicitly] public bool? Browsable = null!;
            [UsedImplicitly] public string? Category = null!;
            [UsedImplicitly] public Action<ConfigEntryBase>? CustomDrawer = null!;
        }

        class AcceptableShortcuts : AcceptableValueBase
        {
            public AcceptableShortcuts() : base(typeof(KeyboardShortcut))
            {
            }

            public override object Clamp(object value) => value;
            public override bool IsValid(object value) => true;

            public override string ToDescriptionString() => "# Acceptable values: " + string.Join(", ", UnityInput.Current.SupportedKeyCodes);
        }

        #endregion
    }


    [HarmonyPatch(typeof(Character), nameof(Character.OnDeath))]
    static class CharacterOnDeathPatch
    {
        public static void Prefix(Character __instance)
        {
            if (__instance.IsPlayer() || __instance.m_deathEffects == null || NoCreatureDustPlugin.RemoveAllVFX.Value == NoCreatureDustPlugin.Toggle.Off)
            {
                return;
            }

            // Remove the vfx from the deathEffects and only the vfx
            EffectList newEffects = new EffectList();
            foreach (EffectList.EffectData? effect in __instance.m_deathEffects.m_effectPrefabs)
            {
                if (effect.m_prefab == null)
                {
                    continue;
                }

                if (effect.m_prefab.name.Contains("vfx_"))
                {
                    continue;
                }

                newEffects.m_effectPrefabs.AddItem(effect);
            }

            __instance.m_deathEffects = newEffects;
        }
    }

    [HarmonyPatch(typeof(Ragdoll), nameof(Ragdoll.DestroyNow))]
    static class RagdollDestroyNowPatch
    {
        static void Prefix(Ragdoll __instance)
        {
            if (__instance.m_removeEffect == null)
            {
                return;
            }

            // Remove the vfx from the deathEffects and only the vfx
            EffectList newEffects = new EffectList();
            foreach (EffectList.EffectData? effect in __instance.m_removeEffect.m_effectPrefabs)
            {
                if (effect.m_prefab == null)
                {
                    continue;
                }

                if (effect.m_prefab.name.Contains("vfx_corpse"))
                {
                    continue;
                }

                if (effect.m_prefab.name.Contains("vfx_") && NoCreatureDustPlugin.RemoveAllRagdollVFX.Value == NoCreatureDustPlugin.Toggle.On)
                {
                    continue;
                }

                newEffects.m_effectPrefabs.AddItem(effect);
            }

            __instance.m_removeEffect = newEffects;
        }
    }
}