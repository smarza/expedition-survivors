using UnityEngine;

namespace ProjectExpedition
{
    public sealed class SurvivorsHudStyles
    {
        public GUIStyle Display;
        public GUIStyle Body;
        public GUIStyle Caption;
        public GUIStyle Micro;
        public GUIStyle StatValue;
        public GUIStyle ConfirmedLabel;
        public GUIStyle Hint;
        public GUIStyle Heading;
        public GUIStyle FilterActive;
        public GUIStyle Title;
        public GUIStyle SectionTitle;
        public GUIStyle FooterButton;

        public static SurvivorsHudStyles Create()
        {
            SurvivorsStylePresentation.EnsureStyles();

            return new SurvivorsHudStyles
            {
                Display = SurvivorsStylePresentation.CreateLabelStyle(
                    34, FontStyle.Bold, SurvivorsStylePresentation.TextGold, TextAnchor.MiddleCenter),
                Body = CreateBody(),
                Caption = SurvivorsStylePresentation.CreateLabelStyle(
                    13, FontStyle.Bold, SurvivorsStylePresentation.TextMuted, TextAnchor.UpperLeft),
                Micro = SurvivorsStylePresentation.CreateLabelStyle(
                    11, FontStyle.Bold, SurvivorsStylePresentation.TextMuted, TextAnchor.MiddleCenter),
                StatValue = SurvivorsStylePresentation.CreateLabelStyle(
                    14, FontStyle.Bold, SurvivorsStylePresentation.TextLight, TextAnchor.MiddleRight),
                ConfirmedLabel = SurvivorsStylePresentation.CreateLabelStyle(
                    24, FontStyle.Bold, SurvivorsStylePresentation.StatPositive, TextAnchor.MiddleCenter),
                Hint = SurvivorsStylePresentation.CreateLabelStyle(
                    12, FontStyle.Bold, SurvivorsStylePresentation.TextMuted, TextAnchor.MiddleCenter),
                Heading = SurvivorsStylePresentation.CreateLabelStyle(
                    22, FontStyle.Bold, SurvivorsStylePresentation.TextGold, TextAnchor.UpperLeft),
                FilterActive = SurvivorsStylePresentation.CreateLabelStyle(
                    12, FontStyle.Bold, SurvivorsStylePresentation.TextGold, TextAnchor.UpperLeft),
                Title = SurvivorsStylePresentation.CreateLabelStyle(
                    42, FontStyle.Bold, SurvivorsStylePresentation.TextGold, TextAnchor.MiddleCenter),
                SectionTitle = SurvivorsStylePresentation.SectionTitleStyle,
                FooterButton = SurvivorsStylePresentation.CreateLabelStyle(
                    18, FontStyle.Bold, SurvivorsStylePresentation.TextLight, TextAnchor.MiddleCenter)
            };
        }

        private static GUIStyle CreateBody()
        {
            var style = SurvivorsStylePresentation.CreateBodyStyle(TextAnchor.UpperLeft);
            style.wordWrap = true;
            style.clipping = TextClipping.Overflow;
            return style;
        }
    }
}
