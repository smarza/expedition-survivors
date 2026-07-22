using UnityEngine;

namespace ProjectExpedition
{
    public static class BiomeCatalog
    {
        public const string FrostboundId = "frostbound";
        public const string CanopyId = "oathbound.canopy";
        public const string RelayId = "ironway.relay";

        public static readonly string[] FrostboundPhaseAnnouncements =
        {
            "THE SHORE AWAKENS",
            "DRIFTWOOD RUN",
            "THE COAST TIGHTENS",
            "THE JOTUNN HAS FOUND YOU",
            "REACH THE BEACON"
        };

        public static readonly string[] CanopyPhaseAnnouncements =
        {
            "THE CANOPY STIRS",
            "ROOT PATH OPEN",
            "THE GROVE TIGHTENS",
            "THE HEARTWOOD AWAKENS",
            "REACH THE CANOPY BEACON"
        };

        public static readonly string[] RelayPhaseAnnouncements =
        {
            "THE RELAY STIRS",
            "SUPPLY LINE OPEN",
            "THE FRONT TIGHTENS",
            "THE SIEGE AUTOMATON ARRIVES",
            "REACH THE EXTRACTION POINT"
        };

        public static string[] ResolvePhaseAnnouncements(string biomeId)
        {
            if (biomeId == CanopyId)
            {
                return CanopyPhaseAnnouncements;
            }

            if (biomeId == RelayId)
            {
                return RelayPhaseAnnouncements;
            }

            return FrostboundPhaseAnnouncements;
        }

        public static string ResolveBossEntranceAnnouncement(string biomeId)
        {
            if (biomeId == CanopyId)
            {
                return "THE HEARTWOOD COLOSSUS CRASHES THROUGH THE CANOPY — HOLD THE LINE";
            }

            if (biomeId == RelayId)
            {
                return "THE SIEGE AUTOMATON CRASHES THROUGH THE RELAY — HOLD THE LINE";
            }

            return "THE JOTUNN WARLORD CRASHES THROUGH THE SHORE — HOLD THE LINE";
        }

        public static string ResolveTwinBossEntranceAnnouncement(string biomeId)
        {
            if (biomeId == CanopyId)
            {
                return "TWIN HEARTWOODS — THE CANOPY SPLITS OPEN";
            }

            if (biomeId == RelayId)
            {
                return "TWIN SIEGE AUTOMATA — THE RELAY BUCKLES";
            }

            return "TWIN JOTUNN — THE SHORE SPLITS OPEN";
        }

        public static string ResolveTwinBossRemainingAnnouncement(string biomeId)
        {
            if (biomeId == CanopyId)
            {
                return "ONE COLOSSUS FALLS — THE OTHER STILL HUNTS";
            }

            if (biomeId == RelayId)
            {
                return "ONE AUTOMATON FALLS — THE OTHER STILL HUNTS";
            }

            return "ONE JOTUNN FALLS — THE OTHER STILL HUNTS";
        }

        public static string ResolveEliteSpawnAnnouncement(string biomeId)
        {
            if (biomeId == CanopyId)
            {
                return "A CANOPY WARDEN EMERGES";
            }

            if (biomeId == RelayId)
            {
                return "A SIGNAL RAIDER EMERGES";
            }

            return "A FROST WRAITH CAPTAIN EMERGES";
        }

        public static string ResolveExtractionUnderwayAnnouncement(string biomeId)
        {
            if (biomeId == CanopyId)
            {
                return "CANOPY EXTRACTION UNDERWAY";
            }

            if (biomeId == RelayId)
            {
                return "RELAY EXTRACTION UNDERWAY";
            }

            return "EXTRACTION UNDERWAY";
        }

        public static string ResolveDeploymentAnnouncement(string biomeId)
        {
            if (biomeId == CanopyId)
            {
                return "RAISING CANOPY CAMP — THE EXPEDITION BEGINS";
            }

            if (biomeId == RelayId)
            {
                return "RAISING RELAY CAMP — THE EXPEDITION BEGINS";
            }

            return "RAISING SHORE CAMP — THE EXPEDITION BEGINS";
        }

        public static string ResolveDeploymentHeadline(string biomeId)
        {
            if (biomeId == CanopyId)
            {
                return "RAISING CANOPY CAMP";
            }

            if (biomeId == RelayId)
            {
                return "RAISING RELAY CAMP";
            }

            return "RAISING SHORE CAMP";
        }

        public static string ResolveDeploymentDetail(string biomeId)
        {
            if (biomeId == CanopyId)
            {
                return "SETTING WATCHFIRES AND MAPPING THE GROVE PATH AHEAD";
            }

            if (biomeId == RelayId)
            {
                return "DEPLOYING GEAR AND OPENING THE SUPPLY LINE";
            }

            return "SECURING SUPPLIES AND SCOUTING THE ROUTE AHEAD";
        }

        public static string ResolveDeploymentObjectiveLabel(string biomeId)
        {
            return "RAISING CAMP";
        }

        public static string ResolveDeploymentCountdownLine(float remainingSeconds) =>
            $"EXPEDITION BEGINS IN {remainingSeconds:0.0}s";
    }
}
