using UnityEngine;

namespace ZeroStarRestaurant.Presentation
{
    // One coordinate system for every provisional HUD block. Smaller windows scale this canvas.
    public readonly struct DebugHudLayout
    {
        public readonly float Width, Height, Scale;
        public readonly Rect Day, Balance, Electricity, Service, Summary, Crosshair, Prompt, Context, Messages;

        public DebugHudLayout(float screenWidth, float screenHeight)
        {
            Scale = Mathf.Min(1f, Mathf.Min(Mathf.Max(1, screenWidth) / 960f, Mathf.Max(1, screenHeight) / 640f));
            Width = Mathf.Max(960, screenWidth / Scale); Height = Mathf.Max(640, screenHeight / Scale);
            float left = Mathf.Clamp(Width * .26f, 240, 340), right = Mathf.Clamp(Width * .29f, 280, 360);
            float middleX = 24 + left, middleWidth = Width - left - right - 48;
            Day = new Rect(12, 12, Width - 24, 52);
            Balance = new Rect(12, 76, left, 108);
            Electricity = new Rect(12, 196, left, Height - 286);
            Service = new Rect(Width - right - 12, 76, right, Height - 166);
            Summary = new Rect(middleX, 76, middleWidth, Height - 166);
            Crosshair = new Rect(Width / 2 - 10, Height / 2 - 12, 20, 24);
            Prompt = new Rect(middleX, Height / 2 + 24, middleWidth, 64);
            Context = new Rect(middleX, Height / 2 + 100, middleWidth, Height / 2 - 190);
            Messages = new Rect(12, Height - 78, Width - 24, 66);
        }
    }
}
