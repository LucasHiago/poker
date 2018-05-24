using System;
using System.Collections.Generic;
using System.Drawing;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Poker
{
	[Flags]
	public enum SpriteEffects
	{
		None = 0,
		FlipV = 1,
		FlipH = 2
	}
	
	public class SpriteBatch : IDisposable
	{
		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		private struct Sprite
		{
			public float X;
			public float Y;
			public float Width;
			public float Height;
			public float SrcX;
			public float SrcY;
			public float SrcWidth;
			public float SrcHeight;
			public byte R;
			public byte G;
			public byte B;
			public byte A;
		}
		
		[DllImport("Native")]
		private static extern IntPtr SB_Create();
		[DllImport("Native")]
		private static extern void SB_Destroy(IntPtr handle);
		[DllImport("Native")]
		private static extern void SB_SetDisplaySize(IntPtr handle, float displayWidth, float displayHeight);
		[DllImport("Native")]
		private static extern void SB_Begin(IntPtr handle);
		[DllImport("Native")]
		private static extern void SB_End(IntPtr handle);
		[DllImport("Native")]
		private static extern void SB_Draw(IntPtr handle, IntPtr texture, ref Sprite sprite);
		
		private readonly IntPtr m_handle;
		
		public SpriteBatch()
		{
			m_handle = SB_Create();
		}
		
		~SpriteBatch()
		{
			SB_Destroy(m_handle);
		}
		
		public void Dispose()
		{
			SB_Destroy(m_handle);
			GC.SuppressFinalize(this);
		}
		
		public void SetDisplaySize(float displayWidth, float displayHeight)
		{
			SB_SetDisplaySize(m_handle, displayWidth, displayHeight);
		}
		
		public void Begin()
		{
			SB_Begin(m_handle);
		}
		
		private static void HFlipRectangle(ref RectangleF rectangle)
		{
			rectangle.X += rectangle.Width;
			rectangle.Width = -rectangle.Width;
		}
		
		private static void VFlipRectangle(ref RectangleF rectangle)
		{
			rectangle.Y += rectangle.Height;
			rectangle.Height = -rectangle.Height;
		}
		
		public void Draw(Texture2D texture, Vector2 position, Color color,
		                 SpriteEffects effects = SpriteEffects.None)
		{
			Draw(texture, new RectangleF(position.X, position.Y, texture.Width, texture.Height), color, effects);
		}
		
		public void Draw(Texture2D texture, RectangleF rectangle, Color color,
		                 SpriteEffects effects = SpriteEffects.None)
		{
			if (effects.HasFlag(SpriteEffects.FlipH))
				HFlipRectangle(ref rectangle);
			if (effects.HasFlag(SpriteEffects.FlipV))
				VFlipRectangle(ref rectangle);
			
			Sprite sprite = new Sprite
			{
				X = rectangle.X,
				Y = rectangle.Y,
				Width = rectangle.Width,
				Height = rectangle.Height,
				SrcX = 0,
				SrcY = 0,
				SrcWidth = texture.Width,
				SrcHeight = texture.Height,
				R = color.R,
				G = color.G,
				B = color.B,
				A = color.A
			};
			
			SB_Draw(m_handle, texture.Handle, ref sprite);
		}
		
		public void Draw(Texture2D texture, Vector2 position, RectangleF srcRectangle, Color color,
		                 SpriteEffects effects = SpriteEffects.None)
		{
			Draw(texture, new RectangleF(position.X, position.Y, srcRectangle.Width, srcRectangle.Height),
			     srcRectangle, color, effects);
		}
		
		public void Draw(Texture2D texture, RectangleF rectangle, RectangleF srcRectangle, Color color,
		                 SpriteEffects effects = SpriteEffects.None)
		{
			if (effects.HasFlag(SpriteEffects.FlipH))
				HFlipRectangle(ref rectangle);
			if (effects.HasFlag(SpriteEffects.FlipV))
				VFlipRectangle(ref rectangle);
			
			Sprite sprite = new Sprite
			{
				X = rectangle.X,
				Y = rectangle.Y,
				Width = rectangle.Width,
				Height = rectangle.Height,
				SrcX = srcRectangle.X,
				SrcY = srcRectangle.Y,
				SrcWidth = srcRectangle.Width,
				SrcHeight = srcRectangle.Height,
				R = color.R,
				G = color.G,
				B = color.B,
				A = color.A
			};
			
			SB_Draw(m_handle, texture.Handle, ref sprite);
		}
		
		public void DrawString(SpriteFont font, string text, Vector2 position, Color color, float scale = 1)
		{
			int x = 0;
			
			for (int i = 0; i < text.Length; i++)
			{
				if (!font.GetCharacter(text[i], out SpriteFont.Character fontChar))
					continue;
				
				int kerning = i > 0 ? font.GetKerning(text[i - 1], text[i]) : 0;
				
				RectangleF rectangle = new RectangleF(position.X + (x + fontChar.XOffset + kerning) * scale,
				                                      position.Y + fontChar.YOffset * scale,
				                                      fontChar.Width * scale, fontChar.Height * scale);
				RectangleF srcRectangle = new RectangleF(fontChar.TextureX, fontChar.TextureY, fontChar.Width, fontChar.Height);
				
				Draw(font.Texture, rectangle, srcRectangle, color);
				
				x += fontChar.XAdvance + kerning;
			}
		}
		
		public void End()
		{
			SB_End(m_handle);
		}
	}
}
