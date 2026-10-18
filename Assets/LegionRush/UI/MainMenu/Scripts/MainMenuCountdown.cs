using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LegionRush
{
    // Pílula escura com relógio e contagem regressiva ("12h 30m"); pode virar um texto de destaque ("ABRIR!").
    public class MainMenuCountdown : MonoBehaviour
    {
        private TMP_Text text;
        private Image clock;

        public static MainMenuCountdown Create(Transform parent)
        {
            Image pill = MainMenuArt.Image(parent, "Panel_TimerPill", "Countdown", true);
            MainMenuCountdown countdown = pill.gameObject.AddComponent<MainMenuCountdown>();
            countdown.clock = MainMenuArt.Image(pill.transform, "Icon_Clock", "Clock");
            UI.Place(countdown.clock.rectTransform, 0.02f, 0.02f, 0.27f, 0.98f);
            countdown.text = MainMenuButtons.Label(pill.transform, "--", 19f, LegionTextStyle.Hud, Color.white);
            UI.Place(countdown.text.rectTransform, 0.27f, 0f, 0.97f, 1f);
            return countdown;
        }

        public void Set(TimeSpan left)
        {
            clock.enabled = true;
            text.color = Color.white;
            text.text = Format(left);
        }

        public void SetMessage(string message, Color color, bool showClock = false)
        {
            clock.enabled = showClock;
            text.color = color;
            text.text = message;
        }

        public static string Format(TimeSpan left)
        {
            if (left.TotalSeconds < 0) left = TimeSpan.Zero;
            if (left.TotalDays >= 1) return $"{(int)left.TotalDays}d {left.Hours}h";
            if (left.TotalHours >= 1) return $"{(int)left.TotalHours}h {left.Minutes:00}m";
            return $"{left.Minutes}m {left.Seconds:00}s";
        }
    }
}
