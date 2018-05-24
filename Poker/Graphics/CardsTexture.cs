using System;
using System.Drawing;

namespace Poker
{
	public class CardsTexture : IDisposable
	{
		public readonly Texture2D Texture;
		
		private const int BORDER_SIZE = 30;
		
		public readonly int CardWidth;
		public readonly int CardHeight;
		
		public CardsTexture()
		{
			Texture = Texture2D.Load("Textures/Cards.png", Texture2D.Type.sRGB32);
			Texture.SetLodBias(-1);
			
			CardWidth = ((int)Texture.Width - BORDER_SIZE * 14) / 13;
			CardHeight = ((int)Texture.Height - BORDER_SIZE * 5) / 4;
		}
		
		public void Bind(int unit)
		{
			Texture.Bind(unit);
		}
		
		public RectangleF GetSourceRectangle(Card card)
		{
			int x = card.Rank == Card.RANK_ACE ? 0 : card.Rank - Card.RANK_2 + 1;
			int y = (int)card.Suit;
			
			return new RectangleF(x * (CardWidth + BORDER_SIZE) + BORDER_SIZE,
				y * (CardHeight + BORDER_SIZE) + BORDER_SIZE, CardWidth, CardHeight);
		}
		
		public void Dispose()
		{
			Texture.Dispose();
		}
	}
}
