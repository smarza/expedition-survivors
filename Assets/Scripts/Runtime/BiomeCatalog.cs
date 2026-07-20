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
    }
}
