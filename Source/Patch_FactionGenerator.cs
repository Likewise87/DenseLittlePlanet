using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace TSA.DenseLittlePlanet
{
    [HarmonyPatch(typeof(FactionGenerator), nameof(FactionGenerator.GenerateFactionsIntoWorldLayer))]
    public static class Patch_FactionGenerator_GenerateFactionsIntoWorldLayer
    {
        public static void Postfix(PlanetLayer layer)
        {
            if (layer == null || !layer.IsRootSurface)
            {
                return;
            }

            if (!ShouldApplyBoost(layer))
            {
                return;
            }

            float densityMultiplier = DenseLittlePlanetMod.Settings.settlementDensityMultiplier;
            if (densityMultiplier <= 1f)
            {
                return;
            }

            World? world = Current.CreatingWorld ?? Find.World;
            if (world?.info == null)
            {
                return;
            }

            WorldInfo worldInfo = world.info;

            List<Faction> eligibleFactions = world.factionManager.AllFactionsListForReading
                .Where(f => IsEligibleFaction(layer, f))
                .ToList();

            if (eligibleFactions.Count == 0)
            {
                return;
            }

            int targetCount = CalculateTargetSettlementCount(layer, worldInfo, densityMultiplier);
            int currentCount = world.worldObjects.AllSettlementsOnLayer(layer).Count;
            int toSpawn = Mathf.Min(
                targetCount - currentCount,
                DenseLittlePlanetSettings.MaxExtraSettlements);

            if (toSpawn <= 0)
            {
                return;
            }

            int consecutiveFailures = 0;
            int spawned = 0;

            for (int i = 0; i < toSpawn; i++)
            {
                Faction faction = eligibleFactions.RandomElementByWeight(f => f.def.settlementGenerationWeight);
                PlanetTile tile = TileFinder.RandomSettlementTileFor(layer, faction);

                if (!tile.Valid || !TileFinder.IsValidTileForNewSettlement(tile))
                {
                    consecutiveFailures++;
                    if (consecutiveFailures >= DenseLittlePlanetSettings.MaxConsecutivePlacementFailures)
                    {
                        break;
                    }

                    continue;
                }

                consecutiveFailures = 0;

                WorldObject settlement = WorldObjectMaker.MakeWorldObject(layer.Def.SettlementWorldObjectDef);
                settlement.SetFaction(faction);
                settlement.Tile = tile;

                if (settlement is INameableWorldObject nameable)
                {
                    nameable.Name = SettlementNameGenerator.GenerateSettlementName(settlement);
                }

                world.worldObjects.Add(settlement);
                spawned++;
            }

            if (spawned < toSpawn && spawned > 0)
            {
                Log.Message($"[DenseLittlePlanet] Placed {spawned}/{toSpawn} extra settlements (planet ran out of valid tiles).");
            }
        }

        private static bool ShouldApplyBoost(PlanetLayer layer)
        {
            if (layer.TilesCount > DenseLittlePlanetSettings.MaxTilesForBoost)
            {
                return false;
            }

            // Skip vanilla-sized worlds (MLP default / no size reduction).
            if (PlanetLayerSettingsDefOf.Surface.settings.subdivisions >= DenseLittlePlanetSettings.VanillaSubdivisions)
            {
                return false;
            }

            return true;
        }

        private static int CalculateTargetSettlementCount(PlanetLayer layer, WorldInfo worldInfo, float densityMultiplier)
        {
            float viewAngleFactor = layer.Def.viewAngleSettlementsFactorCurve.Evaluate(Mathf.Clamp01(layer.ViewAngle / 180f));
            float settlementsPer100k = layer.Def.settlementsPer100kTiles.RandomInRange;
            float populationScale = worldInfo.overallPopulation.GetScaleFactor();

            return GenMath.RoundRandom(
                layer.TilesCount / 100000f * settlementsPer100k * populationScale * viewAngleFactor * densityMultiplier);
        }

        private static bool IsEligibleFaction(PlanetLayer layer, Faction faction)
        {
            if (faction.def.isPlayer || faction.Hidden || faction.temporary)
            {
                return false;
            }

            return CanExistOnLayer(layer, faction.def);
        }

        private static bool CanExistOnLayer(PlanetLayer layer, FactionDef factionDef)
        {
            if (!factionDef.layerBlacklist.NullOrEmpty() && factionDef.layerBlacklist.Contains(layer.Def))
            {
                return false;
            }

            if (!factionDef.layerWhitelist.NullOrEmpty() || !layer.IsRootSurface)
            {
                return factionDef.layerWhitelist.Contains(layer.Def);
            }

            return true;
        }
    }
}
