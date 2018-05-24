using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;

namespace Poker
{
	public class EndSummary : IDisposable
	{
		private class PlayerEntry : IComparable<PlayerEntry>
		{
			public Player player;
			public Net.IClient client;
			public Hands.Hand hand;
			public int winnings;
			
			public PlayerEntry(Player player, Net.Connection connection)
			{
				this.player = player;
				client = connection.GetClientById(player.ClientId);
				
				if (client.RevealedPocketCards != null)
				{
					Card[] cards = client.RevealedPocketCards.Concat(connection.CommunityCards).ToArray();
					hand = Hands.Hand.CreateBest(cards);
				}
			}
			
			public int CompareTo(PlayerEntry other)
			{
				if (hand == null)
					return other.hand == null ? 0 : -1;
				if (other.hand == null)
					return 1;
				return hand.CompareTo(other.hand);
			}
		};
		
		private PlayerEntry[] m_players;
		private Net.Connection m_connection;
		
		private RectangleF m_rectangle;
		private RectangleF m_continueRectangle;
		private Vector2 m_continueButtonSize;
		private Vector2 m_continueTextOffset;
		private Vector2 m_continueTextSize;
		private float m_continueTextScale;
		
		private float m_continueHoverAnimation;
		
		private const string CONTINUE_TEXT = "CONTINUE";
		
		private readonly SpriteBatch m_spriteBatch;
		
		public bool Visible { get; private set; }
		
		private const float WIDTH  = 600 * UI.SCALE;
		private const float HEIGHT = 800 * UI.SCALE;
		private const float Y_OFFSET = 100 * UI.SCALE;
		private const float PADDING = 20 * UI.SCALE;
		
		private const string TITLE_STRING = "HAND SUMMARY";
		private const float TITLE_HEIGHT = 40 * UI.SCALE;
		private Vector2 m_titleSize;
		private float m_titleScale;
		private Vector2 m_titlePos;
		
		private const float PLAYER_LABEL_HEIGHT = 35 * UI.SCALE;
		private float m_playerTextScale;
		
		private const float STATUS_TEXT_HEIGHT = 30 * UI.SCALE;
		private float m_statusTextScale;
		
		public EndSummary()
		{
			m_titleSize = Assets.BoldFont.MeasureString(TITLE_STRING);
			m_titleScale = TITLE_HEIGHT / m_titleSize.Y;
			m_titleSize *= m_titleScale;
			
			m_playerTextScale = PLAYER_LABEL_HEIGHT / Assets.RegularFont.LineHeight;
			m_statusTextScale = STATUS_TEXT_HEIGHT / Assets.RegularFont.LineHeight;
			
			m_spriteBatch = new SpriteBatch();
			
			m_continueButtonSize = new Vector2(Assets.SmallButton2Texture.Width, Assets.SmallButton2Texture.Height) * UI.SCALE;
			
			m_continueTextSize = Assets.RegularFont.MeasureString(CONTINUE_TEXT);
			m_continueTextScale = (UI.TEXT_HEIGHT_PERCENTAGE * m_continueButtonSize.Y) / m_continueTextSize.Y;
			m_continueTextSize *= m_continueTextScale;
			
			m_continueTextOffset = (m_continueButtonSize - m_continueTextSize) / 2;
		}
		
		public void Show(Net.Connection connection)
		{
			Visible = true;
			
			m_players = connection.Players.Select(player => new PlayerEntry(player, connection))
			            .OrderByDescending(i => i).ToArray();
			
			m_connection = connection;
			
			//Calculates winnings
			for (int pot = 0;; pot++)
			{
				int size = 0;
				int contributors = 0;
				int firstContributor = -1;
				
				for (int i = 0; i < m_players.Length; i++)
				{
					if (pot < m_players[i].player.ContributionAmounts.Count)
					{
						int contribution = m_players[i].player.ContributionAmounts[pot];
						if (contribution > 0)
						{
							if (!m_players[i].player.HasFolded)
							{
								if (firstContributor == -1)
									firstContributor = i;
								contributors++;
							}
							size += contribution;
						}
					}
				}
				
				if (size == 0)
					break;
				
				int numWinners = 1;
				for (int i = firstContributor + 1; i < m_players.Length; i++)
				{
					if (m_players[i].CompareTo(m_players[i - 1]) != 0)
						break;
					numWinners++;
				}
				
				int winnings = size / numWinners;
				int rem = size % numWinners;
				
				for (int i = 0; i < numWinners; i++)
				{
					m_players[firstContributor + i].winnings += winnings;
					if (i < rem)
						m_players[firstContributor + i].winnings++;
				}
			}
		}
		
		public void OnResize(int width, int height)
		{
			m_spriteBatch.SetDisplaySize(width, height);
			
			const float CONTINUE_BUTTON_PADDING = 20 * UI.SCALE;
			
			float rectHeight = height * 0.75f;
			m_rectangle = new RectangleF((width - WIDTH) / 2, (height - rectHeight) / 2, WIDTH,
			                             rectHeight - m_continueButtonSize.Y - CONTINUE_BUTTON_PADDING);
			m_titlePos = new Vector2(m_rectangle.Left + (m_rectangle.Width - m_titleSize.X) / 2, m_rectangle.Y + PADDING);
			
			m_continueRectangle = new RectangleF((width - m_continueButtonSize.X) / 2,
			                                     m_rectangle.Bottom + CONTINUE_BUTTON_PADDING,
			                                     m_continueButtonSize.X, m_continueButtonSize.Y);
		}
		
		private static readonly string[] RANK_LABELS = { "2", "3", "4", "5", "6", "7", "8", "9", "10", "J", "Q", "K", "A" };
		
		private static Texture2D GetSuitTexture(Suits suit)
		{
			switch (suit)
			{
			case Suits.Clubs:    return Assets.MiniClubsTexture;
			case Suits.Diamonds: return Assets.MiniDiamondsTexture;
			case Suits.Spades:   return Assets.MiniSpadesTexture;
			case Suits.Hearts:   return Assets.MiniHeartsTexture;
			default: return null;
			}
		}
		
		public void Update(float dt, MouseState ms, MouseState prevMS, out bool continuePressed)
		{
			continuePressed = false;
			if (!Visible)
				return;
			
			if (m_continueRectangle.Contains(ms.Position))
			{
				UI.AnimateInc(ref m_continueHoverAnimation, dt);
				
				if (ms.LeftButton == ButtonState.Pressed && prevMS.LeftButton == ButtonState.Released)
				{
					Visible = false;
					continuePressed = true;
					
					foreach (PlayerEntry player in m_players)
					{
						player.player.Chips += player.winnings;
					}
				}
			}
			else
			{
				UI.AnimateDec(ref m_continueHoverAnimation, dt);
			}
		}
		
		public void Draw()
		{
			if (!Visible)
				return;
			
			m_spriteBatch.Begin();
			
			Color backColor = UI.DEFAULT_BUTTON_COLOR.ScaleAlpha(0.9f);
			m_spriteBatch.Draw(Assets.PixelTexture, m_rectangle, backColor);
			
			m_spriteBatch.DrawString(Assets.BoldFont, TITLE_STRING, m_titlePos, Color.White, m_titleScale);
			
			const float CARD_PADDING = 20 * UI.SCALE;
			const float SPACING_Y = 30 * UI.SCALE;
			
			const float CARDS_WIDTH_PERCENTAGE = 0.8f;
			
			float innerWidth = m_rectangle.Width - PADDING * 2;
			float cardsWidth = innerWidth * CARDS_WIDTH_PERCENTAGE;
			float cardWidth = (cardsWidth + CARD_PADDING) / 5 - CARD_PADDING;
			float cardHeight = cardWidth * Assets.MiniBackTexture.Height / Assets.MiniBackTexture.Width;
			float cardsBeginX = m_rectangle.X + PADDING + (innerWidth - cardsWidth) / 2;
			
			const float CARD_PADDING_PERCENTAGE = 0.1f;
			const float LABEL_HEIGHT_PERCENTAGE = 0.35f;
			const float ICON_HEIGHT_PERCENTAGE = 0.4f;
			
			float cardPadding = cardHeight * CARD_PADDING_PERCENTAGE;
			float labelHeight = cardHeight * LABEL_HEIGHT_PERCENTAGE;
			float iconHeight = cardHeight * ICON_HEIGHT_PERCENTAGE;
			
			float y = TITLE_HEIGHT + PADDING + m_rectangle.Y;
			for (int i = 0; i < m_players.Length; i++)
			{
				if (m_players[i].hand == null)
					continue;
				
				float x = cardsBeginX;
				
				string name = m_players[i].client.Name + ":";
				Vector2 nameSize = Assets.RegularFont.MeasureString(name) * m_playerTextScale;
				
				string handName = m_players[i].hand.Name;
				Vector2 handNameSize = Assets.BoldFont.MeasureString(handName) * m_playerTextScale;
				
				const float HAND_NAME_SPACING = 10 * UI.SCALE;
				float textWidth = nameSize.X + HAND_NAME_SPACING + handNameSize.X;
				float textBeginX = m_rectangle.X + (m_rectangle.Width - textWidth) / 2;
				
				//Draws the player name
				m_spriteBatch.DrawString(Assets.RegularFont, name, new Vector2(textBeginX, y),
				                         Color.White, m_playerTextScale);
				
				//Draws the hand name
				float handNameX = textBeginX + nameSize.X + HAND_NAME_SPACING;
				m_spriteBatch.DrawString(Assets.BoldFont, handName, new Vector2(handNameX, y),
				                         m_players[i].hand.Color, m_playerTextScale);
				
				y += PLAYER_LABEL_HEIGHT + SPACING_Y * 0.3f;
				
				foreach (Card card in m_players[i].hand.Cards)
				{
					bool isPocketCard = m_players[i].client.RevealedPocketCards.Contains(card);
					
					//Draws the card background
					Color cardBackColor = isPocketCard ? new Color(255, 255, 148) : Color.White;
					RectangleF rectangle = new RectangleF(x, y, cardWidth, cardHeight);
					m_spriteBatch.Draw(Assets.MiniBackTexture, rectangle, cardBackColor);
					
					//Measures the size of the rank text
					string text = RANK_LABELS[card.Rank];
					Vector2 textSize = Assets.BoldFont.MeasureString(text);
					float textScale = labelHeight / textSize.Y;
					textSize *= textScale;
					
					Color textColor = Color.Black;
					if (card.Suit == Suits.Diamonds || card.Suit == Suits.Hearts)
						textColor.R = 255;
					
					//Draws the rank text
					float textX = rectangle.X + (rectangle.Width - textSize.X) / 2;
					float textY = rectangle.Y + cardPadding;
					m_spriteBatch.DrawString(Assets.BoldFont, text, new Vector2(textX, textY), textColor, textScale);
					
					//Draws the suit icon
					Texture2D icon = GetSuitTexture(card.Suit);
					float iconY = rectangle.Y + rectangle.Height - cardPadding - iconHeight;
					float iconScale = iconHeight / (float)icon.Height;
					float iconX = rectangle.X + (rectangle.Width - icon.Width * iconScale) / 2;
					m_spriteBatch.Draw(icon, new RectangleF(iconX, iconY, icon.Width * iconScale, iconHeight), Color.White);
					
					x += rectangle.Width + CARD_PADDING;
				}
				
				y += cardHeight;
				
				string statusText = null;
				Color statusColor = new Color();
				if (m_players[i].winnings > 0)
				{
					statusText = string.Format("Wins {0}", m_players[i].winnings);
					statusColor = new Color(151, 252, 158);
				}
				else if (m_players[i].player.Chips == 0)
				{
					statusText = "Bust";
					statusColor = new Color(217, 49, 37);
				}
				
				if (statusText != null)
				{
					y += SPACING_Y * 0.3f;
					
					Vector2 size = Assets.RegularFont.MeasureString(statusText) * m_statusTextScale;
					float statusX = m_rectangle.X + (m_rectangle.Width - size.X) / 2;
					m_spriteBatch.DrawString(Assets.RegularFont, statusText, new Vector2(statusX, y),
					                         statusColor, m_statusTextScale);
					
					y += size.Y;
				}
				
				y += SPACING_Y;
			}
			
			Graphics.SetScissorRectangle((int)m_rectangle.X, (int)m_rectangle.Y,
			                             (int)m_rectangle.Width, (int)m_rectangle.Height);
			Graphics.SetFixedFunctionState(FFState.AlphaBlend | FFState.ScissorTest);
			m_spriteBatch.End();
			
			Graphics.SetFixedFunctionState(FFState.AlphaBlend);
			m_spriteBatch.Begin();
			
			Color buttonColor = Color.Lerp(UI.DEFAULT_BUTTON_COLOR, UI.HOVERED_BUTTON_COLOR, m_continueHoverAnimation);
			m_spriteBatch.Draw(Assets.SmallButton2Texture, m_continueRectangle, buttonColor);
			
			Vector2 textPos = new Vector2(m_continueRectangle.X, m_continueRectangle.Y) + m_continueTextOffset;
			m_spriteBatch.DrawString(Assets.RegularFont, CONTINUE_TEXT, textPos, Color.White, m_continueTextScale);
			
			m_spriteBatch.End();
		}
		
		public void Dispose()
		{
			m_spriteBatch.Dispose();
		}
	}
}