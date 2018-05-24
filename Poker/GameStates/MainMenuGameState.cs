using System;
using System.Drawing;
using System.Net;
using System.Numerics;

namespace Poker
{
	public class MainMenuGameState : GameState, IDisposable
	{
		private readonly RectangleF[] m_buttonRectangles = new RectangleF[3];
		private float m_targetTextHeight;
		
		private readonly float[] m_buttonBrightness = new float[3];
		
		private bool m_joinGameMenuOpen;
		private float m_joinGameOpenProgress;
		
		private bool m_hostGameMenuOpen;
		private float m_hostGameOpenProgress;
		
		private float m_closeProgress;
		private Action m_closeAction;
		
		private float m_errorOpacity;
		private string m_errorMessage;
		
		private const float ERROR_MESSAGE_HEIGHT = 35 * UI.SCALE;
		private readonly float m_errorMessageScale = ERROR_MESSAGE_HEIGHT / Assets.BoldFont.LineHeight;
		private Vector2 m_errorMessagePos;
		
		private RectangleF m_joinServerIpLabelRectangle;
		private RectangleF m_joinNicknameLabelRectangle;
		
		private RectangleF m_hostNicknameLabelRectangle;
		
		private readonly Texture2D m_serverIpLabelTexture;
		private readonly Texture2D m_nicknameLabelTexture;
		
		private readonly TextBox m_joinServerIpTextBox = new TextBox();
		private readonly TextBox m_joinNicknameTextBox = new TextBox();
		private readonly MenuButton m_joinBackButton = new MenuButton("BACK");
		private readonly MenuButton m_joinConnectButton = new MenuButton("CONNECT");
		
		private readonly TextBox m_hostNicknameTextBox = new TextBox();
		private readonly MenuButton m_hostBackButton = new MenuButton("BACK");
		private readonly MenuButton m_hostButton = new MenuButton("HOST");
		
		public delegate void HostGameCallback(string nickname);
		public delegate void JoinGameCallback(IPAddress serverIp, string nickname);
		
		public event HostGameCallback HostGame;
		public event JoinGameCallback JoinGame;
		
		public MainMenuGameState()
		{
			m_serverIpLabelTexture = Texture2D.Load("UI/ServerIpLabel.png");
			m_nicknameLabelTexture = Texture2D.Load("UI/NicknameLabel.png");
		}
		
		public override void OnResize(int newWidth, int newHeight)
		{
			const float ELEMENT_PADDING = 10 * UI.SCALE;
			
			float buttonWidth = UI.SCALE * Assets.ButtonTexture.Width;
			float buttonHeight = UI.SCALE * Assets.ButtonTexture.Height;
			
			float smallButtonWidth = UI.SCALE * Assets.SmallButtonTexture.Width * 0.75f;
			float smallButtonHeight = UI.SCALE * Assets.SmallButtonTexture.Height * 0.75f;
			
			float tbxWidth = UI.SCALE * Assets.TextBoxBackTexture.Width;
			float tbxHeight = UI.SCALE * Assets.TextBoxBackTexture.Height;
			
			// ** Lays out elements on the main menu **
			{
				float totalHeight = (buttonHeight + ELEMENT_PADDING) * m_buttonRectangles.Length - ELEMENT_PADDING;
				
				m_targetTextHeight = buttonHeight * UI.TEXT_HEIGHT_PERCENTAGE;
				
				float beginY = (newHeight - totalHeight) / 2;
				for (int i = 0; i < m_buttonRectangles.Length; i++)
				{
					m_buttonRectangles[i] = new RectangleF(0, beginY, buttonWidth, buttonHeight);
					beginY += buttonHeight + ELEMENT_PADDING;
				}
			}
			
			int beginX2 = (int)(buttonWidth + 50 * UI.SCALE);
			
			// ** Lays out elements on the join menu **
			{
				float totalHeight = (m_serverIpLabelTexture.Height + m_nicknameLabelTexture.Height) * UI.SCALE +
				                    smallButtonHeight + ELEMENT_PADDING * 2;
				float beginY = (newHeight - totalHeight) / 2;
				
				float serverIpHeight = m_serverIpLabelTexture.Height * UI.SCALE;
				m_joinServerIpLabelRectangle = new RectangleF(beginX2, beginY, m_serverIpLabelTexture.Width * UI.SCALE, serverIpHeight);
				m_joinServerIpTextBox.Rectangle = new RectangleF(m_joinServerIpLabelRectangle.Right, beginY, tbxWidth, tbxHeight);
				beginY += serverIpHeight + ELEMENT_PADDING;
				
				float nicknameHeight = m_nicknameLabelTexture.Height * UI.SCALE;
				m_joinNicknameLabelRectangle = new RectangleF(beginX2, beginY, m_serverIpLabelTexture.Width * UI.SCALE, nicknameHeight);
				m_joinNicknameTextBox.Rectangle = new RectangleF(m_joinNicknameLabelRectangle.Right, beginY, tbxWidth, tbxHeight);
				beginY += nicknameHeight + ELEMENT_PADDING;
				
				float connectButtonX = beginX2 + smallButtonWidth;
				m_joinBackButton.Rectangle = new RectangleF(beginX2, beginY, smallButtonWidth, smallButtonHeight);
				m_joinConnectButton.Rectangle = new RectangleF(connectButtonX, beginY, smallButtonWidth, smallButtonHeight);
				
				m_errorMessagePos = new Vector2(connectButtonX + smallButtonWidth + UI.SCALE * 10,
					beginY + (smallButtonHeight - ERROR_MESSAGE_HEIGHT) / 2);
			}
			
			// ** Lays out elements on the host menu **
			{
				float totalHeight = m_nicknameLabelTexture.Height * UI.SCALE + smallButtonHeight + ELEMENT_PADDING * 2;
				float beginY = (newHeight - totalHeight) / 2;
				
				float nicknameHeight = m_nicknameLabelTexture.Height * UI.SCALE;
				m_hostNicknameLabelRectangle = new RectangleF(beginX2, beginY,
					m_serverIpLabelTexture.Width * UI.SCALE, nicknameHeight);
				m_hostNicknameTextBox.Rectangle = new RectangleF(m_hostNicknameLabelRectangle.Right, beginY,
					tbxWidth, tbxHeight);
				beginY += nicknameHeight + ELEMENT_PADDING;
				
				float hostButtonX = beginX2 + smallButtonWidth;
				m_hostBackButton.Rectangle = new RectangleF(beginX2, beginY, smallButtonWidth, smallButtonHeight);
				m_hostButton.Rectangle = new RectangleF(hostButtonX, beginY, smallButtonWidth, smallButtonHeight);
			}
		}
		
		public override void Activated()
		{
			m_joinGameMenuOpen = false;
			m_joinGameOpenProgress = 0;
			m_hostGameMenuOpen = false;
			m_hostGameOpenProgress = 0;
			m_closeAction = null;
		}
		
		public override void OnKeyPress(Keys key)
		{
			if (m_joinGameMenuOpen)
			{
				m_joinServerIpTextBox.KeyPress(key);
				m_joinNicknameTextBox.KeyPress(key);
			}
			
			if (m_hostGameMenuOpen)
			{
				m_hostNicknameTextBox.KeyPress(key);
			}
		}
		
		public override void OnTextInput(string text)
		{
			if (m_joinGameMenuOpen)
			{
				m_joinServerIpTextBox.TextInput(text);
				m_joinNicknameTextBox.TextInput(text);
			}
			
			if (m_hostGameMenuOpen)
			{
				m_hostNicknameTextBox.TextInput(text);
			}
		}
		
		public void SetError(string message)
		{
			m_errorMessage = message;
			m_errorOpacity = 0;
			
			if (message != null)
			{
				m_hostGameMenuOpen = false;
				m_joinGameMenuOpen = true;
			}
		}
		
		private MouseState m_prevMS;
		private KeyboardState m_prevKS;
		
		public override void Update(float dt)
		{
			bool spinFast = m_joinNicknameTextBox.Text.Equals("Gunnar", StringComparison.InvariantCultureIgnoreCase);
			MenuBackground.Instance.Update(dt, spinFast);
			
			MouseState ms = MouseState.GetCurrent();
			KeyboardState ks = KeyboardState.GetCurrent();
			
			if (m_prevMS == null)
				m_prevMS = ms;
			if (m_prevKS == null)
				m_prevKS = ks;
			
			if (m_closeAction != null)
			{
				UI.AnimateInc(ref m_closeProgress, dt);
				if (m_closeProgress > 1.0f - 1E-6f)
				{
					m_closeAction();
					m_closeAction = null;
					m_closeProgress = 0;
				}
			}
			
			int hoveredIndex = -1;
			
			for (int i = 0; i < m_buttonRectangles.Length; i++)
			{
				bool hovered = m_buttonRectangles[i].Contains(ms.Position);
				
				if (hovered)
				{
					hoveredIndex = i;
					UI.AnimateInc(ref m_buttonBrightness[i], dt);
				}
				else
				{
					UI.AnimateDec(ref m_buttonBrightness[i], dt);
				}
			}
			
			if (m_joinGameMenuOpen)
			{
				UI.AnimateInc(ref m_joinGameOpenProgress, dt);
				
				m_joinNicknameTextBox.Update(dt);
				m_joinServerIpTextBox.Update(dt);
				
				if (m_joinBackButton.Update(dt, ms, m_prevMS))
					m_joinGameMenuOpen = false;
				
				bool canConnect = !m_joinServerIpTextBox.Empty && !m_joinNicknameTextBox.Empty;
				if (m_joinConnectButton.Update(dt, ms, m_prevMS, canConnect))
				{
					IPAddress serverIp;
					if (!IPAddress.TryParse(m_joinServerIpTextBox.Text, out serverIp))
					{
						SetError("Invalid host IP");
					}
					else
					{
						m_closeAction = () => { JoinGame?.Invoke(serverIp, m_joinNicknameTextBox.Text); };
						m_closeProgress = 0;
						SetError(null);
					}
				}
				
				//Changes to the next text box if the user pressed tab
				if (ks.IsKeyDown(Keys.Tab) && !m_prevKS.IsKeyDown(Keys.Tab))
				{
					if (m_joinServerIpTextBox.HasFocus)
					{
						m_joinServerIpTextBox.HasFocus = false;
						m_joinNicknameTextBox.HasFocus = true;
					}
				}
			}
			else
			{
				UI.AnimateDec(ref m_joinGameOpenProgress, dt);
			}
			
			if (m_hostGameMenuOpen)
			{
				UI.AnimateInc(ref m_hostGameOpenProgress, dt);
				
				m_hostNicknameTextBox.Update(dt);
				
				if (m_hostBackButton.Update(dt, ms, m_prevMS))
					m_hostGameMenuOpen = false;
				
				bool canHost = !m_hostNicknameTextBox.Empty;
				if (m_hostButton.Update(dt, ms, m_prevMS, canHost))
				{
					m_closeAction = () => { HostGame?.Invoke(m_hostNicknameTextBox.Text); };
					m_closeProgress = 0;
				}
			}
			else
			{
				UI.AnimateDec(ref m_hostGameOpenProgress, dt);
			}
			
			if (ms.LeftButton == ButtonState.Pressed && m_prevMS.LeftButton == ButtonState.Released)
			{
				if (m_joinGameMenuOpen)
				{
					m_joinNicknameTextBox.HasFocus = m_joinNicknameTextBox.Rectangle.Contains(ms.Position);
					m_joinServerIpTextBox.HasFocus = m_joinServerIpTextBox.Rectangle.Contains(ms.Position);
					
					m_joinServerIpTextBox.MouseClick(ms.Position.ToVector2());
					m_joinNicknameTextBox.MouseClick(ms.Position.ToVector2());
				}
				
				if (m_hostGameMenuOpen)
				{
					m_hostNicknameTextBox.HasFocus = m_hostNicknameTextBox.Rectangle.Contains(ms.Position);
					m_hostNicknameTextBox.MouseClick(ms.Position.ToVector2());
				}
				
				if (hoveredIndex != -1)
				{
					if (m_joinGameMenuOpen)
						m_joinGameMenuOpen = false;
					else if (m_hostGameMenuOpen)
						m_hostGameMenuOpen = false;
					else
					{
						switch (hoveredIndex)
						{
							case 0:
								m_hostGameMenuOpen = true;
								m_hostGameOpenProgress = 0.0f;
								break;
							case 1:
								m_joinGameMenuOpen = true;
								m_joinGameOpenProgress = 0.0f;
								break;
							case 2:
								Program.ExitGame();
								break;
						}
					}
				}
			}
			
			if (m_errorMessage != null)
				UI.AnimateInc(ref m_errorOpacity, dt, 0.75f);
			else
				m_errorOpacity = 0;
			
			m_prevKS = ks;
			m_prevMS = ms;
		}
		
		private static readonly string[] BUTTON_LABELS = { "HOST GAME", "JOIN GAME", "QUIT" };
		
		public override void Draw(DrawArgs drawArgs)
		{
			MenuBackground.Instance.Draw();
			
			drawArgs.SpriteBatch.Begin();
			
			float alphaScale = 1.0f - m_closeProgress;
			
			//Draws the first menu screen
			bool menu1Disabled = m_joinGameMenuOpen || m_hostGameMenuOpen;
			float disableProgress = Math.Max(m_joinGameOpenProgress, m_hostGameOpenProgress);
			for (int i = 0; i < m_buttonRectangles.Length; i++)
			{
				Color color;
				if (menu1Disabled)
					color = Color.Lerp(UI.DEFAULT_BUTTON_COLOR, UI.DISABLED_BUTTON_COLOR, disableProgress);
				else
					color = Color.Lerp(UI.DEFAULT_BUTTON_COLOR, UI.HOVERED_BUTTON_COLOR, m_buttonBrightness[i]);
				color.A = (byte)(color.A * alphaScale);
				
				RectangleF rectangle = m_buttonRectangles[i];
				rectangle.X += Math.Min(-disableProgress, m_buttonBrightness[i] - 1) * 10;
				
				drawArgs.SpriteBatch.Draw(Assets.ButtonTexture, rectangle, color);
				
				Vector2 textSize = Assets.RegularFont.MeasureString(BUTTON_LABELS[i]);
				float textScale = m_targetTextHeight / textSize.Y;
				textSize *= textScale;
				
				Vector2 textPos = rectangle.Center() - textSize / 2;
				
				Color textColor = new Color(1, 1, 1, alphaScale);
				drawArgs.SpriteBatch.DrawString(Assets.RegularFont, BUTTON_LABELS[i], textPos, textColor, textScale);
			}
			
			const float FADE_TRANSLATE_DIST = 40 * UI.SCALE;
			
			//Draws the join game screen
			if (m_joinGameOpenProgress > 0)
			{
				Color color = new Color(1.0f, 1.0f, 1.0f, m_joinGameOpenProgress * alphaScale);
				int xOffset = (int)((m_joinGameOpenProgress - 1) * FADE_TRANSLATE_DIST);
				
				RectangleF serverIpRect = m_joinServerIpLabelRectangle;
				serverIpRect.X += xOffset;
				drawArgs.SpriteBatch.Draw(m_serverIpLabelTexture, serverIpRect, color);
				
				m_joinServerIpTextBox.Draw(drawArgs.SpriteBatch, xOffset, m_joinGameOpenProgress * alphaScale);
				
				RectangleF nicknameRect = m_joinNicknameLabelRectangle;
				nicknameRect.X += xOffset;
				drawArgs.SpriteBatch.Draw(m_nicknameLabelTexture, nicknameRect, color);
				
				m_joinNicknameTextBox.Draw(drawArgs.SpriteBatch, xOffset, m_joinGameOpenProgress * alphaScale);
				
				m_joinBackButton.Draw(drawArgs.SpriteBatch, xOffset, m_joinGameOpenProgress * alphaScale);
				m_joinConnectButton.Draw(drawArgs.SpriteBatch, xOffset, m_joinGameOpenProgress * alphaScale);
				
				if (m_errorOpacity > 0)
				{
					drawArgs.SpriteBatch.DrawString(Assets.BoldFont, m_errorMessage, m_errorMessagePos + new Vector2(xOffset, 0),
						new Color(1.0f, 0.3f, 0.1f, m_errorOpacity * alphaScale), m_errorMessageScale);
				}
			}
			
			//Draws the host game screen
			if (m_hostGameOpenProgress > 0)
			{
				Color color = new Color(1.0f, 1.0f, 1.0f, m_hostGameOpenProgress * alphaScale);
				int xOffset = (int)((m_hostGameOpenProgress - 1) * FADE_TRANSLATE_DIST);
				
				RectangleF nicknameRect = m_hostNicknameLabelRectangle;
				nicknameRect.X += xOffset;
				drawArgs.SpriteBatch.Draw(m_nicknameLabelTexture, nicknameRect, color);
				
				m_hostNicknameTextBox.Draw(drawArgs.SpriteBatch, xOffset, m_hostGameOpenProgress * alphaScale);
				
				m_hostBackButton.Draw(drawArgs.SpriteBatch, xOffset, m_hostGameOpenProgress * alphaScale);
				m_hostButton.Draw(drawArgs.SpriteBatch, xOffset, m_hostGameOpenProgress * alphaScale);
			}
			
			Graphics.SetFixedFunctionState(FFState.AlphaBlend);
			drawArgs.SpriteBatch.End();
		}
		
		public void Dispose()
		{
			m_serverIpLabelTexture.Dispose();
			m_nicknameLabelTexture.Dispose();
		}
	}
}
