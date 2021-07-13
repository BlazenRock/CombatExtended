﻿using System;
using System.Linq;
using System.Runtime.CompilerServices;
using RimWorld;
using Verse;

namespace CombatExtended
{
    public static class DefUtility
    {
        /// <summary>
        /// A bitmap that store flags. The real size of this one is 2048 byte.
        /// </summary>
        internal static FlagArray isVisibleLayerArray = new FlagArray(ushort.MaxValue);

        /// <summary>
        /// A bitmap that store flags. The real size of this one is 2048 byte.
        /// </summary>
        internal static FlagArray isAOEArray = new FlagArray(ushort.MaxValue);

        // <summary>
        /// A bitmap that store flags. The real size of this one is 2048 byte.
        /// </summary>
        internal static FlagArray isMenuHiddenArray = new FlagArray(ushort.MaxValue);

        /// <summary>
        /// Used to create and initialize def related flags that are often checked but require more than 2 or 3 steps to caculate.
        /// Should be only called when all defs are loaded.
        /// </summary>
        public static void Initialize()
        {
            // Process all apparel defs
            foreach (ApparelLayerDef layer in DefDatabase<ApparelLayerDef>.AllDefs)
                ProcessApparelLayer(layer);

            // Process all apparel defs
            foreach (ThingDef def in DefDatabase<ThingDef>.AllDefs.Where(t => t.IsApparel))
                ProcessApparel(def);

            // Process all defs for isMenuHiddenArray
            foreach (ThingDef def in DefDatabase<ThingDef>.AllDefs.Where(t => t.HasModExtension<ThingDefExtensionCE>()))
                ProcessThingDefExtensionCE(def);

            // Process all defs for ammo to find AOE defs
            foreach (AmmoSetDef def in DefDatabase<AmmoSetDef>.AllDefs)
                ProcessAmmo(def);

            // Process all weapons
            foreach (ThingDef def in DefDatabase<ThingDef>.AllDefs.Where(d => d.HasComp(typeof(CompAmmoUser))))
                ProcessWeapons(def);
        }

        /// <summary>
        /// Check is this apparel is in shell or an outer visible layer
        /// </summary>
        /// <param name="def">Apparel def</param>
        /// <returns>Visability</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsVisibleLayer(this ThingDef def)
        {
            if (!def.IsApparel) throw new ArgumentException("Argument need to be apparel!");
            return isVisibleLayerArray[def.index];
        }

        /// <summary>
        /// Check is this apparellayer is in shell or an outer visible layer
        /// </summary>
        /// <param name="def">Apparel layer def</param>
        /// <returns>Visability</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsVisibleLayer(this ApparelLayerDef def)
        {
            return isVisibleLayerArray[def.index];
        }

        /// <summary>
        /// Check is this ThingDef is MenuHidden. This replace the old removed menuHidden field.
        /// </summary>
        /// <param name="def">Thing def</param>
        /// <returns>Is menu hidden</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsMenuHidden(this ThingDef def)
        {
            return isMenuHiddenArray[def.index];
        }

        /// <summary>
        /// Used to update is menuhidden value.
        /// </summary>
        /// <param name="def">Thing def</param>
        /// <param name="value">New value</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetMenuHidden(this ThingDef def, bool value)
        {
            isMenuHiddenArray[def.index] = value;

            if (def.HasModExtension<ThingDefExtensionCE>()) // Check if this def has ThingDefExtensionCE
                def.GetModExtension<ThingDefExtensionCE>().MenuHidden = value;
        }

        /// <summary>
        /// Return wether this ThingDef is an AOE weapon def
        /// </summary>
        /// <param name="def">Weapon def</param>
        /// <returns>If this ThingDef is an AOE weapon</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsAOEWeapon(this ThingDef def)
        {
            return isAOEArray[def.index];
        }

        /// <summary>
        /// Prepare apparel def by caching isVisibleLayer.
        /// </summary>
        /// <param name="def">Apparel def</param>        
        private static void ProcessApparel(ThingDef def)
        {
            ApparelLayerDef layer = def.apparel.LastLayer;

            isVisibleLayerArray[def.index] = isVisibleLayerArray[layer.index]; // set isVisibleLayerArray from the layer index since layers are processed first.
        }

        /// <summary>
        /// Prepare apparellayerdef def by caching isVisibleLayer.
        /// </summary>
        /// <param name="def">Apparel def</param>        
        private static void ProcessApparelLayer(ApparelLayerDef layer)
        {
            isVisibleLayerArray[layer.index] = IsVisibleLayer_Internal(layer);
        }

        /// <summary>
        /// Used for rendering of CE custom apparel layers.
        /// </summary>
        /// <param name="layer"></param>
        /// <returns></returns>        
        private static bool IsVisibleLayer_Internal(ApparelLayerDef layer)
        {
            /* 
             * Belt is not actually a pre-shell layer, but we want to treat it as such in this patch,
             * to avoid rendering bugs with utility items (e.g: broadshield pack)                        
             */
            return true
                && layer.drawOrder >= ApparelLayerDefOf.Shell.drawOrder
                && layer != ApparelLayerDefOf.Belt
                && !(layer.GetModExtension<ApparelLayerExtension>()?.IsHeadwear ?? false);
        }

        /// <summary>
        /// Perpare the ThingDefExtensionCE.
        /// </summary>
        /// <param name="def">ThingDef with </param>        
        private static void ProcessThingDefExtensionCE(ThingDef def)
        {
            ThingDefExtensionCE ext = def.GetModExtension<ThingDefExtensionCE>();

            isMenuHiddenArray[def.index] = ext.MenuHidden;
        }

        /// <summary>
        /// Process weapon by checking if any ammo used by it is AOE
        /// </summary>
        /// <param name="def"></param>
        private static void ProcessWeapons(ThingDef def)
        {
            CompProperties_AmmoUser props = (CompProperties_AmmoUser)def.comps.First(c => c.compClass == typeof(CompAmmoUser));

            isAOEArray[def.index] = isAOEArray[props.ammoSet.index];
        }

        /// <summary>
        /// Process and check if this ammoSet is for a AOE weapons
        /// </summary>
        /// <param name="def">AmmoSetDef</param>
        private static void ProcessAmmo(AmmoSetDef def)
        {
            /*
             * Check is it's a mortar projectile then check if detonateProjectile is not null
             * This should cover most types of AOE ammo
             */
            isAOEArray[def.index] = def.isMortarAmmoSet || (def.ammoTypes?.Any(a => IsAOEAmmoLink(a)) ?? false);
        }

        /// <summary>
        /// Determine of given ammoLink is an AOE Ammo
        /// </summary>
        /// <param name="link">AmmoLink</param>
        /// <returns>If ammo is AOE</returns>
        private static bool IsAOEAmmoLink(AmmoLink link)
        {
            return link.ammo?.detonateProjectile != null
                || link.projectile?.thingClass == typeof(ProjectileCE_Explosive)
                || link.projectile?.thingClass == typeof(Projectile_Explosive)
                || (link.projectile?.comps?.Any(c => c.compClass == typeof(CompFragments) || c.compClass == typeof(CompExplosive) || c.compClass == typeof(CompExplosiveCE)) ?? false);
        }
    }
}