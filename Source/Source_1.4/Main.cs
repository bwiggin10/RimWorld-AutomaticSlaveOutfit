using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using System.Reflection;
using Verse;

namespace AutomaticSlaveOutfit
{
    [StaticConstructorOnStartup]
    public static class AutomaticSlaveOutfit
    {
        private static void AutoSlaveOutfit(Pawn pawn)
        {
            if (pawn.GuestStatus == GuestStatus.Slave &&
                pawn.outfits != null &&
                !WorldComp.EnslavedPawns.Contains(pawn))
            {
                List<Outfit> allOutfits = Current.Game.outfitDatabase.AllOutfits;

                foreach (var outfit in allOutfits)
                {
                    if (outfit.label == "Slave")
                    {
                        pawn.outfits.CurrentOutfit = outfit;
                    }
                    WorldComp.EnslavedPawns.Add(pawn);
                }

            }
        }

        [HarmonyPatch(typeof(Thing), nameof(Thing.SpawnSetup))]
        public static class Patch_Thing_SpawnSetup
        {
            // Patching for initial pawns
            public static void Postfix(Thing __instance)
            {
                if (__instance is Pawn p && p.Faction?.IsPlayer == true && p.def?.race?.Humanlike == true)
                {
                    AutoSlaveOutfit(p);
                }
            }
        }

        [HarmonyPatch(typeof(InteractionWorker_EnslaveAttempt), nameof(InteractionWorker_EnslaveAttempt.Interacted))]
        public static class Patch_InteractionWorker_EnslaveAttempt
        {
            // Patching for enslaved prisoners
            public static void Postfix(Pawn initiator, Pawn recipient)
            {
                if (recipient is Pawn p && p.GuestStatus == GuestStatus.Slave)
                {
                    AutoSlaveOutfit(p);
                }
            }
        }

        static AutomaticSlaveOutfit()
        {
            Harmony harmony = new Harmony("AutomaticSlaveOutfit_Ben");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }

        public static class OutfitLabel
        {
            public static Outfit Slave;
        }

    }

    class WorldComp : WorldComponent
    {
        // Using a HashSet for quick lookup
        public static HashSet<Pawn> EnslavedPawns = new HashSet<Pawn>();
        // I've found it easier to have a null list for use when exposing data
        // and HashSet will fail if more than one null value is added.
        private List<Pawn> usedForExposingData = null;

        public WorldComp(World w) : base(w)
        {
            // Make sure the static HashSet is cleared whenever a game is created or loaded.
            EnslavedPawns.Clear();
        }

        public override void ExposeData()
        {
            base.ExposeData();
            if (Scribe.mode == LoadSaveMode.Saving)
            {
                // When saving, populate the list
                usedForExposingData = new List<Pawn>(EnslavedPawns);
            }

            Scribe_Collections.Look(ref usedForExposingData, "enslavedPawns", LookMode.Reference);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                // When loading, clear the HashSet then populate it with the loaded data
                EnslavedPawns.Clear();
                foreach (var v in usedForExposingData)
                {
                    // Remove any null records
                    if (v != null)
                    {
                        EnslavedPawns.Add(v);
                    }
                }
            }

            if (Scribe.mode == LoadSaveMode.Saving ||
                Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                // Add hints to the garbage collector that this memory can be collected
                usedForExposingData?.Clear();
                usedForExposingData = null;
            }
        }
    }
}