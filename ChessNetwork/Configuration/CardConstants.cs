namespace ChessNetwork.Configuration
{
    // Definiert statische Konstanten für Karten-IDs und andere kartenbezogene Werte.
    // Dies vermeidet "magische Strings" im Code und erhöht die Typsicherheit.
    public static class CardConstants
    {
        // IDs für Gameplay-Karten
        public const string ExtraZug = "extrazug";
        public const string Teleport = "teleport";
        public const string Positionstausch = "positionstausch";
        public const string AddTime = "addtime";
        public const string SubtractTime = "subtracttime";
        public const string TimeSwap = "timeswap";
        public const string Wiedergeburt = "wiedergeburt";
        public const string CardSwap = "cardswap";
        public const string SacrificeEffect = "opfergabe";

        // Konstanten für spezielle oder Fallback-Karten
        public const string FallbackCardIdPrefix = "card_fallback_";
        public const string NoMoreCardsName = "Keine Karten mehr";
        public const string ReplacementCardName = "Ersatzkarte";
        public const string DefaultCardBackImageUrl = "img/cards/templateback.png";
    }
}