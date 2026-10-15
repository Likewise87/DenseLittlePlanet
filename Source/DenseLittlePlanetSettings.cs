using UnityEngine;
using Verse;

namespace TSA.DenseLittlePlanet
{
    public sealed class DenseLittlePlanetSettings : ModSettings
    {
        public const float MinDensityMultiplier = 1f;
        public const float MaxDensityMultiplier = 40f;
        public const float DefaultDensityMultiplier = 10f;

        /// <summary>Only boost worlds at or below this tile count (tiny/small MLP worlds).</summary>
        public const int MaxTilesForBoost = 20000;

        /// <summary>Vanilla default subdivisions — at or above this, the boost is skipped entirely.</summary>
        public const int VanillaSubdivisions = 10;

        /// <summary>Hard cap on extra settlements spawned per world generation.</summary>
        public const int MaxExtraSettlements = 200;

        /// <summary>Stop trying after this many consecutive failed tile picks.</summary>
        public const int MaxConsecutivePlacementFailures = 5;

        public float settlementDensityMultiplier = DefaultDensityMultiplier;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref settlementDensityMultiplier, "settlementDensityMultiplier", DefaultDensityMultiplier);
            settlementDensityMultiplier = ClampDensity(settlementDensityMultiplier);
        }

        public static float ClampDensity(float value)
        {
            return Mathf.Clamp(value, MinDensityMultiplier, MaxDensityMultiplier);
        }
    }
}
