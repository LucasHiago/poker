using System.Drawing;
using System.Numerics;

namespace Poker
{
	public class MenuButton
	{
		public readonly string Label;
		
		public RectangleF Rectangle { get; set; }
		
		private float m_disableProgress;
		private float m_brighness;
		
		public MenuButton(string label)
		{
			Label = label;
		}
		
		public bool Update(float dt, MouseState currentMS, MouseState prevMS, bool enabled = true)
		{
			bool clicked = false;
			
			if (!enabled)
			{
				UI.AnimateInc(ref m_disableProgress, dt);
			}
			else
			{
				UI.AnimateDec(ref m_disableProgress, dt);
				
				if (Rectangle.Contains(currentMS.Position))
				{
					UI.AnimateInc(ref m_brighness, dt);
					
					if (currentMS.LeftButton == ButtonState.Pressed && prevMS.LeftButton == ButtonState.Released)
					{
						clicked = true;
					}
				}
				else
				{
					UI.AnimateDec(ref m_brighness, dt);
				}
			}
			
			return clicked;
		}
		
		public void Draw(SpriteBatch spriteBatch, int xOffset = 0, float alpha = 1)
		{
			Color backColor;
			if (m_disableProgress > 0)
				backColor = Color.Lerp(UI.DEFAULT_BUTTON_COLOR, UI.DISABLED_BUTTON_COLOR, m_disableProgress);
			else
				backColor = Color.Lerp(UI.DEFAULT_BUTTON_COLOR, UI.HOVERED_BUTTON_COLOR, m_brighness);
			backColor.A = (byte)(backColor.A * alpha);
			
			RectangleF rectangle = Rectangle;
			rectangle.X += xOffset;
			
			spriteBatch.Draw(Assets.SmallButtonTexture, rectangle, backColor);
			
			float textHeight = rectangle.Height * UI.TEXT_HEIGHT_PERCENTAGE;
			Vector2 labelSize = Assets.RegularFont.MeasureString(Label);
			float textScale = textHeight / labelSize.Y;
			
			spriteBatch.DrawString(Assets.RegularFont, Label, rectangle.Center() - labelSize * textScale * 0.5f,
				new Color(1, 1, 1, alpha), textScale);
		}
	}
}
