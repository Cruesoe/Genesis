using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace Genesis
{
    // Non-pausing strip of era badges near the top of the screen: fades in, scrolls to the new era, holds, fades out
    public class Window_EraAdvance : Window
    {
        private const float FadeInDuration = 1f;
        private const float HoldDuration = 3f;
        private const float FadeOutDuration = 1f;
        private const float TotalDuration = FadeInDuration + HoldDuration + FadeOutDuration;

        private const float StripHeight = 190f;
        private const float BadgeSlot = 150f;
        private const float BadgeMaxSize = 130f;
        private const float TopPositionFraction = 0.26f;

        private static readonly List<TechLevel> Eras = new List<TechLevel>
        {
            TechLevel.Animal, TechLevel.Neolithic, TechLevel.Medieval, TechLevel.Industrial, TechLevel.Spacer, TechLevel.Ultra,
        };

        private static readonly Dictionary<TechLevel, Color> EraColors = new Dictionary<TechLevel, Color>
        {
            { TechLevel.Animal, new Color(0.55f, 0.42f, 0.28f) },
            { TechLevel.Neolithic, new Color(0.40f, 0.55f, 0.25f) },
            { TechLevel.Medieval, new Color(0.55f, 0.50f, 0.45f) },
            { TechLevel.Industrial, new Color(0.70f, 0.42f, 0.18f) },
            { TechLevel.Spacer, new Color(0.25f, 0.45f, 0.70f) },
            { TechLevel.Ultra, new Color(0.55f, 0.30f, 0.65f) },
        };

        private readonly TechLevel newLevel;
        private readonly int targetIndex;
        private readonly float startTime;
        private float scrollPositionX;

        public Window_EraAdvance(TechLevel level)
        {
            newLevel = level;
            targetIndex = Eras.IndexOf(level);
            scrollPositionX = (targetIndex - 1) * BadgeSlot;
            doCloseX = false;
            forcePause = false;
            preventCameraMotion = false;
            closeOnClickedOutside = true;
            doWindowBackground = false;
            drawShadow = false;
            startTime = Time.realtimeSinceStartup;
            SoundDefOf.Quest_Succeded.PlayOneShotOnCamera();
        }

        public static string EraLabel(TechLevel level) => ("Genesis_Era_" + level).Translate();

        public override Vector2 InitialSize => new Vector2(UI.screenWidth, StripHeight);

        protected override float Margin => 0f;

        protected override void SetInitialSizeAndPosition() =>
            windowRect = new Rect(0f, UI.screenHeight * TopPositionFraction - StripHeight, UI.screenWidth, StripHeight);

        public override void DoWindowContents(Rect inRect)
        {
            var elapsed = Time.realtimeSinceStartup - startTime;
            if (elapsed >= TotalDuration)
            {
                Close();
                return;
            }

            float alpha;
            if (elapsed < FadeInDuration)
            {
                alpha = Mathf.Clamp01(elapsed / FadeInDuration);
            }
            else if (elapsed < FadeInDuration + HoldDuration)
            {
                alpha = 1f;
            }
            else
            {
                alpha = 1f - Mathf.Clamp01((elapsed - FadeInDuration - HoldDuration) / FadeOutDuration);
            }

            GUI.color = new Color(0f, 0f, 0f, alpha * 0.5f);
            GUI.DrawTexture(inRect, BaseContent.WhiteTex);

            scrollPositionX = Mathf.Lerp(scrollPositionX, targetIndex * BadgeSlot, Time.deltaTime * 2f);
            var centerX = inRect.width / 2f;
            var centerY = 70f;

            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleCenter;
            for (var i = 0; i < Eras.Count; i++)
            {
                var xPos = centerX + i * BadgeSlot - scrollPositionX - BadgeSlot / 2f;
                var distance = Mathf.Abs(centerX - (xPos + BadgeSlot / 2f));
                var scale = Mathf.Clamp(1f - distance / 375f, 0.5f, 1f);
                var badgeAlpha = Mathf.Clamp(1f - distance / 450f, 0.2f, 1f) * alpha;

                var size = BadgeMaxSize * scale;
                var badgeRect = new Rect(xPos + (BadgeSlot - size) / 2f, centerY - size / 2f, size, size);
                var color = EraColors[Eras[i]];
                Widgets.DrawBoxSolid(badgeRect, new Color(color.r, color.g, color.b, badgeAlpha));
                GUI.color = new Color(1f, 1f, 1f, badgeAlpha);
                Widgets.DrawBox(badgeRect, 2);
                if (scale > 0.7f)
                {
                    Widgets.Label(badgeRect, EraLabel(Eras[i]));
                }
            }

            Text.Font = GameFont.Medium;
            GUI.color = new Color(1f, 1f, 1f, alpha);
            Widgets.Label(new Rect(0f, centerY + 90f, inRect.width, 35f), "Genesis_ColonyIsNowEra".Translate(EraLabel(newLevel)));
            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Small;
            GUI.color = Color.white;
        }
    }
}
