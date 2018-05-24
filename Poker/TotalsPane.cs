using System;
using System.Collections.Generic;
using System.Drawing;
using System.Numerics;

namespace Poker
{
	class TotalsPane
	{
		private float m_width;
		private float m_height;
		private float m_y;
		
		private float m_enterProgress;
		
		private const string TITLE = "TOTALS";
		private const float TITLE_WIDTH_PERCENTAGE = 0.5f;
		
		private readonly Vector2 m_titleSize;
		private float m_titleScale;
		private Vector2 m_titlePos;
		private float m_titleEndY;
		
		private float m_itemPadding;
		private float m_textScale;
		
		public TotalsPane()
		{
			m_titleSize = Assets.RegularFont.MeasureString(TITLE);
		}
		
		public void OnResize(int width, int height)
		{
			const float HEIGHT_PERCENTAGE = 0.75f;
			const float ASPECT_RATIO = 2.5f;
			
			m_height = height * HEIGHT_PERCENTAGE;
			m_width = m_height / ASPECT_RATIO;
			m_y = (height - m_height) / 2;
			
			float padding = 0.05f * m_height;
			
			float titleWidth = m_width * TITLE_WIDTH_PERCENTAGE;
			m_titlePos = new Vector2((m_width - titleWidth) / 2, m_y + padding);
			m_titleScale = titleWidth / m_titleSize.X;
			m_titleEndY = m_titlePos.Y + m_titleScale * m_titleSize.Y + padding;
			m_itemPadding = padding / 2;
			
			float targetTextHeight = m_height * 0.04f;
			m_textScale = targetTextHeight / Assets.RegularFont.LineHeight;
		}
		
		public void Update(bool visible, float dt)
		{
			const float ANIMATION_SPEED = 0.75f;
			if (visible)
				UI.AnimateInc(ref m_enterProgress, dt, ANIMATION_SPEED);
			else
				UI.AnimateDec(ref m_enterProgress, dt, ANIMATION_SPEED);
		}
		
		public void Draw(SpriteBatch spriteBatch, Net.Connection connection)
		{
			if (m_enterProgress <= 0)
				return;
			
			float progress = Utils.SmoothStep(m_enterProgress);
			
			float x = (progress - 1) * m_width;
			
			Color backColor = UI.DEFAULT_BUTTON_COLOR.ScaleAlpha(0.8f * progress);
			spriteBatch.Draw(Assets.PixelTexture, new RectangleF(x, m_y, m_width, m_height), backColor);
			
			Color textColor = Color.White.ScaleAlpha(progress);
			spriteBatch.DrawString(Assets.RegularFont, TITLE, new Vector2(m_titlePos.X + x, m_titlePos.Y),
			                       textColor, m_titleScale);
			
			Color moneyColor = new Color(255, 184, 107, (int)(progress * 255));
			
			float y = m_titleEndY;
			
			void DrawTotalsEntry(string label, string value, float alpha = 1)
			{
				Vector2 labelSize = Assets.RegularFont.MeasureString(label) * m_textScale;
				Vector2 valueSize = Assets.BoldFont.MeasureString(value) * m_textScale;
				
				float itemHeight = Math.Max(labelSize.Y, valueSize.Y);
				float inflateY = m_itemPadding * 0.25f;
				RectangleF backRect = new RectangleF(x, y - inflateY, m_width, itemHeight + inflateY * 2);
				spriteBatch.Draw(Assets.PixelTexture, backRect, new Color(0, 0, 0, 0.1f * progress));
				
				spriteBatch.DrawString(Assets.RegularFont, label, new Vector2(m_itemPadding + x, y),
				                       textColor.ScaleAlpha(alpha), m_textScale);
				spriteBatch.DrawString(Assets.BoldFont, value, new Vector2(x + m_width - m_itemPadding - valueSize.X, y),
				                       moneyColor.ScaleAlpha(alpha), m_textScale);
				
				y += itemHeight + m_itemPadding;
			}
			
			//Draws pot sizes
			for (int pot = 0;; pot++)
			{
				int size = connection.GameDriver.GetPotSize(pot);
				if (size == 0 && pot != 0)
					break;
				
				string label = pot == 0 ? "Main Pot:" : string.Format("Side Pot {0}:", pot);
				DrawTotalsEntry(label, size.ToString());
			}
			
			y += m_itemPadding;
			
			//Draws player chip counts
			foreach (Player player in connection.GameDriver.Players)
			{
				DrawTotalsEntry(connection.GetClientById(player.ClientId).Name + ":",
				                player.Chips.ToString(), player.Bust ? 0.5f : 1.0f);
			}
		}
	}
}