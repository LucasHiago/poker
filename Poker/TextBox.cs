using System;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Text;

namespace Poker
{
	public class TextBox
	{
		public RectangleF Rectangle { get; set; }
		
		public bool HasFocus { get; set; }
		private float m_focusProgress;
		
		private int m_caretPosition;
		private readonly StringBuilder m_textBuilder = new StringBuilder();
		
		public string Text => m_textBuilder.ToString();
		public bool Empty => m_textBuilder.Length == 0;
		
		private const float CARET_BLINK_TIME = 0.8f;
		private float m_caretTime;
		
		private float TextBeginX => Rectangle.X + Rectangle.Height * 0.75f;
		private float TextHeight => Rectangle.Height * UI.TEXT_HEIGHT_PERCENTAGE;
		
		public void Update(float dt)
		{
			if (!HasFocus && m_focusProgress > 0)
			{
				m_focusProgress -= dt * UI.ANIMATION_SPEED;
				if (m_focusProgress < 0)
					m_focusProgress = 0;
			}
			
			if (HasFocus && m_focusProgress < 1)
			{
				m_focusProgress += dt * UI.ANIMATION_SPEED;
				if (m_focusProgress > 1)
					m_focusProgress = 1;
			}
			
			m_caretTime = (m_caretTime + dt / CARET_BLINK_TIME) % 1.0f;
		}
		
		public void MouseClick(Vector2 position)
		{
			if (!HasFocus || !Rectangle.Contains(position))
				return;
			
			float xOffset = position.X - TextBeginX;
			if (xOffset < 0)
			{
				m_caretPosition = 0;
				return;
			}
			
			string text = m_textBuilder.ToString();
			Vector2 textSize = Assets.RegularFont.MeasureString(text);
			float textScale = TextHeight / textSize.Y;
			
			float prevTextWidth = 0;
			for (int i = 1; i < text.Length; i++)
			{
				float textWidth = Assets.RegularFont.MeasureString(text.Substring(0, i)).X * textScale;
				if (xOffset <= textWidth)
				{
					if (Math.Abs(xOffset - prevTextWidth) < Math.Abs(xOffset - textWidth))
						m_caretPosition = i - 1;
					else
						m_caretPosition = i;
					return;
				}
				prevTextWidth = textWidth;
			}
			
			m_caretPosition = text.Length;
		}
		
		public void KeyPress(Keys key)
		{
			if (!HasFocus)
				return;
			
			switch (key)
			{
			case Keys.Left:
				if (m_caretPosition > 0)
				{
					m_caretPosition--;
					m_caretTime = 0;
				}
				break;
			case Keys.Right:
				if (m_caretPosition < m_textBuilder.Length)
				{
					m_caretPosition++;
					m_caretTime = 0;
				}
				break;
			case Keys.Home:
				if (m_caretPosition != 0)
				{
					m_caretPosition = 0;
					m_caretTime = 0;
				}
				break;
			case Keys.End:
				if (m_caretPosition != m_textBuilder.Length)
				{
					m_caretPosition = m_textBuilder.Length;
					m_caretTime = 0;
				}
				break;
			case Keys.Backspace:
				if (m_caretPosition > 0)
				{
					m_textBuilder.Remove(--m_caretPosition, 1);
					m_caretTime = 0;
				}
				break;
			case Keys.Delete:
				if (m_caretPosition < m_textBuilder.Length)
				{
					m_textBuilder.Remove(m_caretPosition, 1);
					m_caretTime = 0;
				}
				break;
			}
		}
		
		public void TextInput(string text)
		{
			if (!HasFocus)
				return;
			
			m_textBuilder.Insert(m_caretPosition++, text.Where(c => Assets.RegularFont.SupportsCharacter(c)).ToArray());
			m_caretTime = 0;
		}
		
		private const int CARET_WIDTH = 1;
		
		public void Draw(SpriteBatch spriteBatch, int xOffset, float alpha)
		{
			Color rimColor = Color.Lerp(UI.DEFAULT_BUTTON_COLOR, UI.HOVERED_BUTTON_COLOR, m_focusProgress * 0.8f);
			rimColor.A = (byte)(rimColor.A * alpha);
			
			RectangleF rectangle = Rectangle;
			rectangle.X += xOffset;
			
			spriteBatch.Draw(Assets.TextBoxBackTexture, rectangle, rimColor);
			spriteBatch.Draw(Assets.TextBoxInnerTexture, rectangle, new Color(1, 1, 1, 0.6f * alpha));
			
			float textScale = 1;
			float textBeginX = TextBeginX + xOffset;
			
			Color textColor = new Color(1, 1, 1, alpha);
			
			string text = m_textBuilder.ToString();
			
			if (text.Length != 0)
			{
				float textHeight = TextHeight;
				Vector2 textSize = Assets.RegularFont.MeasureString(text);
				textScale = textHeight / textSize.Y;
				
				float textBeginY = rectangle.Y + (rectangle.Height - textHeight) / 2;
				spriteBatch.DrawString(Assets.RegularFont, text, new Vector2(textBeginX, textBeginY),
					textColor, textScale);
			}
			
			if (HasFocus && m_caretTime < 0.5f)
			{
				string pString = text.Substring(0, m_caretPosition);
				float pStringWidth = Assets.RegularFont.MeasureString(pString).X * textScale;
				
				float caretHeight = rectangle.Height * UI.TEXT_HEIGHT_PERCENTAGE * 1.1f;
				RectangleF caretRectangleF = new RectangleF((int)(textBeginX + pStringWidth),
					(int)(rectangle.Y + (rectangle.Height - caretHeight) / 2), CARET_WIDTH, (int)caretHeight);
				RectangleF caretSrcRectangleF = new RectangleF(Assets.TextBoxBackTexture.Width / 2,
					0, 1, Assets.TextBoxBackTexture.Height);
				
				spriteBatch.Draw(Assets.TextBoxBackTexture, caretRectangleF, caretSrcRectangleF, textColor);
			}
		}
	}
}
