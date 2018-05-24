using System;

namespace Poker
{
	public static class UI
	{
		public const float SCALE = 0.75f;
		
		public static readonly Color DEFAULT_BUTTON_COLOR = new Color(61, 68, 94);
		public static readonly Color DISABLED_BUTTON_COLOR = new Color(61, 68, 94, 200);
		public static readonly Color HOVERED_BUTTON_COLOR = new Color(255, 83, 123);
		public const float ANIMATION_SPEED = 10;
		public const float TEXT_HEIGHT_PERCENTAGE = 0.5f;
		
		public static void AnimateInc(ref float progress, float dt, float speed = 1)
		{
			progress = Math.Min(progress + dt * ANIMATION_SPEED * speed, 1.0f);
		}
		
		public static void AnimateDec(ref float progress, float dt, float speed = 1)
		{
			progress = Math.Max(progress - dt * ANIMATION_SPEED * speed, 0.0f);
		}
	}
}
