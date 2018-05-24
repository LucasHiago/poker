using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Text;
using Poker.Net;

namespace Poker
{
	public class MainGameState : GameState, IDisposable
	{
		private const float FOV = MathF.PI * 0.45f;
		private const float Z_NEAR = 0.1f;
		private const float Z_FAR = 1000.0f;
		private const float CAMERA_PITCH = 0.4f * MathF.PI;
		private const float CAMERA_DIST = 3.5f;
		
		private Matrix4x4 m_viewMatrix;
		private Matrix4x4 m_projectionMatrix;
		
		private static readonly Vector3 LIGHT_DIRECTION = Vector3.Normalize(new Vector3(0.5f, -0.75f, 1.0f));
		
		private readonly Texture2D m_keyboardGuideTexture;
		
		private float m_cameraYaw;
		private float m_cameraPitch;
		
		private KeyboardState m_prevKS;
		private MouseState m_prevMS;
		
		private int m_displayWidth;
		private int m_displayHeight;
		
		private Connection m_connection;
		
		private int m_communityCardsRevealed;
		
		private float m_initialDealProgress;
		private float m_communityCardRevealProgress;
		private float m_communityCardFocusProgress;
		
		private float m_actionButtonsAlpha;
		private RectangleF m_callButtonRectangle;
		private RectangleF m_foldButtonRectangle;
		private RectangleF m_betUpButtonRectangle;
		private RectangleF m_betDownButtonRectangle;
		private Vector2 m_callAmountPosition;
		
		private RectangleF m_keyboardGuideRectangle;
		
		private float m_callButtonHighlight;
		private float m_foldButtonHighlight;
		private float m_betUpButtonHighlight;
		private float m_betDownButtonHighlight;
		private float m_betUpButtonDisProgress;
		private float m_betDownButtonDisProgress;
		private float m_betButtonCooldown;
		
		private int m_callAmount;
		private int m_raiseAmount;
		
		private struct LogMessage
		{
			public string Source;
			public string Message;
		};
		
		private float m_logFadeInProgress = 1;
		private float m_logScrollY = 0;
		private int m_logAreaHeight;
		private readonly List<LogMessage> m_log = new List<LogMessage>();
		
		private const float LOG_LINE_HEIGHT = 30 * UI.SCALE;
		private float LogHeight => LOG_LINE_HEIGHT * (m_log.Count - 1 + m_logFadeInProgress);
		
		private readonly float[] m_pocketCardsY = new float[2];
		private readonly bool[] m_isDraggingPocketCards = new bool[2];
		private float m_pocketCardDragOffset;
		
		private readonly float[] m_pocketCardsX = new float[2];
		private float m_pocketCardWidth;
		private float m_pocketCardHeight;
		
		private volatile bool m_waitingForHandStart = true;
		
		private float m_pocketCardRevealProgress;
		
		private const float HAND_SUMMARY_REVEAL_DELAY = 1;
		private float m_handSummaryRevealProgress;
		
		private float m_blurIntensity;
		
		private class PlayerEntry
		{
			public Player Player;
			public IClient Client;
			
			public float HighlightIntensity;
			public float FoldIntensity;
			
			public readonly List<float> OutStackRotations = new List<float>();
			public readonly List<float> InStackRotations = new List<float>();
			
			public int NumMovingChips;
			public float ChipsMoveProgress;
			
			public Color TextColor =>
				Color.Lerp(Color.Lerp(new Color(255, 255, 255, 200), new Color(247, 200, 45), HighlightIntensity),
						   new Color(255, 255, 255, 50), FoldIntensity);
			
			public Vector3 BoardAreaCenter;
			public Vector3 BoardAreaUp;
			public Vector3 BoardAreaLeft;
			
			public float[] CardRotations;
		}
		
		private PlayerEntry[] m_players;
		private int m_selfPlayerIndex;
		private bool m_flipNames;
		
		private IClient m_winner;
		private RectangleF m_returnToMenuButtonRect;
		private float m_returnToMenuButtonHighlight;
		
		private readonly PlayerNameRenderer m_playerNameRenderer = new PlayerNameRenderer();
		private readonly ShadowMapper m_shadowMapper = new ShadowMapper(LIGHT_DIRECTION);
		private readonly EndSummary m_endSummary = new EndSummary();
		private readonly TotalsPane m_totalsPane = new TotalsPane();
		
		private readonly ViewProjUniformBuffer m_viewProjUniformBuffer = new ViewProjUniformBuffer();
		
		/* Nested array containing which player positions are used for different number of players.
		 * Each inner array is a list of position slots which are to be used.
		 * There are 10 position slots around the board:
		 *     0 1 2
		 *    -------
		 * 7 |       | 3
		 *    -------
		 *     6 5 4
		 */
		private static readonly int[][] PLAYER_POSITION_SLOTS =
		{
			/* 2 Players  */ new [] { 1, 5 },
			/* 3 Players  */ new [] { 0, 2, 5 },
			/* 4 Players  */ new [] { 0, 2, 4, 6 },
			/* 5 Players  */ new [] { 0, 2, 4, 5, 6 },
			/* 6 Players  */ new [] { 0, 1, 2, 4, 5, 6 },
			/* 7 Players  */ new [] { 0, 1, 2, 3, 4, 6, 7 },
			/* 8 Players  */ new [] { 0, 1, 2, 3, 4, 5, 6, 7 }
		};
		
		private static readonly Vector2[] POSITION_SLOT_TEXT_POSITIONS =
		{
			new Vector2(1.75f, -1.5f), new Vector2(1.75f, 0), new Vector2(1.75f, 1.5f),
			new Vector2(0.0f, 2.75f),
			new Vector2(-1.75f, 1.5f), new Vector2(-1.75f, 0), new Vector2(-1.75f, -1.5f),
			new Vector2(0.0f, -2.75f)
		};
		
		private static readonly Vector2[] POSITION_SLOT_LEFT_VECTORS =
		{
			new Vector2(0, -1), new Vector2(0, -1), new Vector2(0, -1), new Vector2(1, 0),
			new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(-1, 0)
		};
		
		private static readonly Vector3 CARD_DECK_POSITION = new Vector3(0.0f, 0.2f, -1.0f);
		private const float CARD_DECK_HEIGHT = 0.05f;
		
		private readonly float[] m_chipEntropy = new float[1024];
		
		private const int STACK_SIZE = 100;
		
		private bool m_waitingForPlayers = false;
		
		public MainGameState()
		{
			m_keyboardGuideTexture = Texture2D.Load("UI/KeyboardGuide.png");
			
			Random random = new Random();
			for (int i = 0; i < m_chipEntropy.Length; i++)
				m_chipEntropy[i] = (float)random.NextDouble();
		}
		
		private void StartHand()
		{
			if (!m_waitingForHandStart)
				return;
			m_communityCardsRevealed = 0;
			m_communityCardRevealProgress = 0;
			m_handSummaryRevealProgress = 0;
			m_pocketCardRevealProgress = 0;
			m_initialDealProgress = 0;
			
			m_callAmount = m_connection.BigBlind;
			m_raiseAmount = 0;
			
			for (int i = 0; i < 2; i++)
				m_pocketCardsY[i] = 0;
			
			const float ROTATION_RANGE = MathF.PI * 0.1f;
			
			Random random = new Random();
			for (int i = 0; i < m_players.Length; i++)
			{
				float rotation1 = Utils.Lerp(-ROTATION_RANGE, ROTATION_RANGE, (float)random.NextDouble());
				float rotation2 = Utils.Lerp(-ROTATION_RANGE, ROTATION_RANGE, (float)random.NextDouble());
				
				m_players[i].CardRotations[0] = rotation1 + MathF.PI / 2;
				m_players[i].CardRotations[1] = rotation2 + MathF.PI / 2;
				m_players[i].Player = m_connection.GetPlayer(m_players[i].Client.ClientId);
				m_players[i].FoldIntensity = 0;
				m_players[i].ChipsMoveProgress = 0;
				
				int inStackSize = m_players[i].Player.Chips;
				
				m_players[i].InStackRotations.Clear();
				m_players[i].OutStackRotations.Clear();
				
				if (m_players[i].InStackRotations.Capacity < inStackSize)
				{
					m_players[i].InStackRotations.Capacity = inStackSize;
				}
				
				for (int j = 0; j < inStackSize; j++)
				{
					m_players[i].InStackRotations.Add((float)(random.NextDouble() * Math.PI * 2));
				}
				
				if (m_players[i].Client.ClientId == m_connection.SelfClientId)
					m_selfPlayerIndex = i;
			}
			
			m_waitingForPlayers = false;
			m_waitingForHandStart = false;
		}
		
		private void WriteLogMessage(string source, string message)
		{
			m_log.Add(new LogMessage { Source = source, Message = message });
			m_logFadeInProgress = 0;
		}
		
		public void SetConnection(Connection connection)
		{
			m_connection = connection;
			m_log.Clear();
			
			m_winner = null;
			
			m_connection.OnTurnChanged += client =>
			{
				if (client != null && client.ClientId == m_connection.SelfClientId)
				{
					m_callAmount = m_connection.CallAmount;
					m_raiseAmount = 0;
				}
			};
			
			connection.OnPlayerAction += (clientId, action, raiseAmount) =>
			{
				PlayerEntry playerEntry = m_players.First(player => player.Client.ClientId == clientId);
				StringBuilder statusTextBuilder = new StringBuilder();
				
				switch (action)
				{
				case TurnEndAction.AllIn:
					statusTextBuilder.AppendFormat(" went all in ({0})", playerEntry.Player.Chips);
					break;
				case TurnEndAction.Raise:
					statusTextBuilder.AppendFormat(" raised by {0}", raiseAmount);
					break;
				case TurnEndAction.Call:
					statusTextBuilder.Append(m_connection.CallAmount == 0 ? " checked" : " called");
					break;
				case TurnEndAction.Fold:
					statusTextBuilder.Append(" folded");
					break;
				}
				
				WriteLogMessage(playerEntry.Client.Name, statusTextBuilder.ToString());
			};
			
			const float TEXT_HEIGHT = 0.25f;
			const float TEXT_Y = 0.201f;
			
			m_players = new PlayerEntry[connection.TurnOrderClients.Count];
			TextMeshBuilder textMeshBuilder = new TextMeshBuilder(Assets.BoldFont);
			
			for (int i = 0; i < m_players.Length; i++)
			{
				if (connection.TurnOrderClients[i].ClientId == connection.SelfClientId)
				{
					m_flipNames = PLAYER_POSITION_SLOTS[m_players.Length - 2][i] >= 4;
					break;
				}
			}
			
			for (int i = 0; i < m_players.Length; i++)
			{
				int positionSlot = PLAYER_POSITION_SLOTS[m_players.Length - 2][i];
				
				Vector2 left2D = POSITION_SLOT_LEFT_VECTORS[positionSlot];
				Vector2 up2D = new Vector2(left2D.Y, -left2D.X);
				
				Vector2 centerBoardArea2D = POSITION_SLOT_TEXT_POSITIONS[positionSlot];
				centerBoardArea2D += up2D * 0.6f;
				
				m_players[i] = new PlayerEntry
				{
					Client = connection.TurnOrderClients[i],
					BoardAreaCenter = new Vector3(centerBoardArea2D.X, 0.1f, centerBoardArea2D.Y),
					BoardAreaUp = new Vector3(up2D.X, 0, up2D.Y),
					BoardAreaLeft = new Vector3(left2D.X, 0, left2D.Y),
					CardRotations = new float[2]
				};
				
				//Adds the player's label to the text builder
				Vector2 textPos = POSITION_SLOT_TEXT_POSITIONS[positionSlot];
				textMeshBuilder.AddText(m_players[i].Client.Name, TEXT_HEIGHT,
					new Vector3(textPos.X, TEXT_Y, textPos.Y), new Vector3(m_flipNames ? -1 : 1, 0, 0),
					new Vector3(0, 0, m_flipNames ? 1 : -1), (uint)i);
			}
			
			m_cameraYaw = (m_flipNames ? 1 : -1) * MathF.PI / 2.0f;
			m_cameraPitch = 0.4f * MathF.PI;
			
			m_playerNameRenderer.SetMesh(Assets.BoldFont, textMeshBuilder.CreateMesh());
			
			m_waitingForHandStart = true;
			
			m_connection.OnDeal += (card1, card2) => StartHand();
			if (!m_connection.WaitingForNetwork)
				StartHand();
		}
		
		public override void Update(float dt)
		{
			if (m_waitingForHandStart)
				return;
			
			KeyboardState ks = KeyboardState.GetCurrent();
			MouseState ms = MouseState.GetCurrent();
			
			m_endSummary.Update(dt, ms, m_prevMS, out bool continuePressed);
			if (continuePressed)
			{
				PlayerEntry[] playersNotBust = m_players.Where(p => p.Player.Chips != 0).ToArray();
				if (playersNotBust.Length == 1)
				{
					m_winner = playersNotBust[0].Client;
				}
				else
				{
					m_waitingForHandStart = true;
					m_waitingForPlayers = true;
					m_connection.NextHand();
				}
			}
			
			if (m_winner != null)
			{
				if (m_returnToMenuButtonRect.Contains(ms.Position))
				{
					if (ms.LeftButton == ButtonState.Pressed && m_prevMS.LeftButton == ButtonState.Released)
					{
						m_connection.Disconnect();
						GameStateManager.SetGameState<MainMenuGameState>();
					}
					
					UI.AnimateInc(ref m_returnToMenuButtonHighlight, dt);
				}
				else
				{
					UI.AnimateDec(ref m_returnToMenuButtonHighlight, dt);
				}
			}
			
			m_totalsPane.Update(!m_endSummary.Visible && ks.IsKeyDown(Keys.F1), dt);
			
			if (ms.RightButton == ButtonState.Pressed && m_communityCardFocusProgress < 1E-3f)
			{
				const float ROTATE_SENSITIVITY = MathF.PI * 1.5f;
				m_cameraYaw += (ms.Position.X - m_prevMS.Position.X) * ROTATE_SENSITIVITY / m_displayWidth;
				m_cameraPitch += (ms.Position.Y - m_prevMS.Position.Y) * ROTATE_SENSITIVITY / m_displayHeight;
				
				const float MIN_PITCH = MathF.PI * 0.1f;
				const float MAX_PITCH = MathF.PI * 0.49f;
				
				if (m_cameraPitch < MIN_PITCH)
					m_cameraPitch = MIN_PITCH;
				if (m_cameraPitch > MAX_PITCH)
					m_cameraPitch = MAX_PITCH;
			}
			
			// ** Log message buffer **
			float logHeight = LogHeight;
			if (logHeight > m_logAreaHeight && m_prevMS != null)
			{
				const float SCROLL_SPEED = 0.75f;
				m_logScrollY += (ms.ScrollY - m_prevMS.ScrollY) * LOG_LINE_HEIGHT * SCROLL_SPEED;
				m_logScrollY = Utils.Clamp(m_logScrollY, 0, logHeight - m_logAreaHeight);
			}
			
			if (m_logFadeInProgress < 1)
			{
				const float LOG_FADE_IN_SPEED = 5;
				m_logFadeInProgress = Math.Min(m_logFadeInProgress + LOG_FADE_IN_SPEED * dt, 1.0f);
			}
			
			const float DEAL_ANIMATION_SPEED = 5;
			
			bool showActionsUI = false;
			
			float maxDealProgress = m_players.Length * 2 + 5;
			bool dealAnimationComplete = m_initialDealProgress >= maxDealProgress;
			bool playerChipsMoving = false;
			bool revealPocketCards = false;
			
			// ** Updates player highlight **
			IClient currentClient = m_connection.CurrentClient;
			foreach (PlayerEntry player in m_players)
			{
				if (player.Player.HasFolded)
				{
					UI.AnimateInc(ref player.FoldIntensity, dt);
				}
				else
				{
					if (player.Client.RevealedPocketCards != null)
						revealPocketCards = true;
					
					if (player.Client == currentClient && dealAnimationComplete)
						UI.AnimateInc(ref player.HighlightIntensity, dt);
					else
						UI.AnimateDec(ref player.HighlightIntensity, dt);
					
					if (dealAnimationComplete)
					{
						if (player.NumMovingChips > 0)
						{
							const float MOVE_SPEED = 3.0f;
							
							player.ChipsMoveProgress += dt * MOVE_SPEED;
							playerChipsMoving = true;
							
							if (player.ChipsMoveProgress >= 1)
							{
								int inStackBegin = player.InStackRotations.Count - player.NumMovingChips;
								player.OutStackRotations.AddRange(
									player.InStackRotations.Skip(player.InStackRotations.Count - player.NumMovingChips));
								player.InStackRotations.RemoveRange(inStackBegin, player.NumMovingChips);
								player.NumMovingChips = 0;
							}
						}
						else if (player.InStackRotations.Count > player.Player.Chips)
						{
							int lastInStackSize = player.InStackRotations.Count % STACK_SIZE;
							if (lastInStackSize == 0)
								lastInStackSize += STACK_SIZE;
							
							int lastOutStackSpace = STACK_SIZE - player.OutStackRotations.Count % STACK_SIZE;
							
							player.ChipsMoveProgress = 0;
							player.NumMovingChips = Math.Min(Math.Min(lastInStackSize, lastOutStackSpace),
								player.InStackRotations.Count - player.Player.Chips);
							
							playerChipsMoving = true;
						}
					}
				}
			}
			
			bool selfFolded = m_players[m_selfPlayerIndex].Player.HasFolded;
			
			if (m_connection.HasDealtPocketCards)
			{
				if (!dealAnimationComplete)
				{
					m_initialDealProgress += dt * DEAL_ANIMATION_SPEED;
					if (m_initialDealProgress > maxDealProgress)
						m_initialDealProgress = maxDealProgress;
				}
				else
				{
					// ** Updates the pocket cards UI **
					
					float defaultY = selfFolded ? 0 : m_pocketCardHeight * 0.05f;
					float maxY = m_pocketCardHeight * 0.25f;
					
					for (int i = 0; i < 2; i++)
					{
						if (m_isDraggingPocketCards[i] && !selfFolded)
						{
							if (ms.LeftButton == ButtonState.Released)
								m_isDraggingPocketCards[i] = false;
							else
							{
								m_pocketCardsY[i] = m_displayHeight - (ms.Position.Y - m_pocketCardDragOffset);
								m_pocketCardsY[i] = Math.Min(Math.Max(m_pocketCardsY[i], defaultY), maxY);
							}
						}
						else
						{
							bool keyExpand = ks.IsKeyDown(Keys.C);
							if (keyExpand)
								defaultY = maxY;
							
							float defaultScreenY = m_displayHeight - defaultY;
							
							if (ms.LeftButton == ButtonState.Pressed && m_prevMS.LeftButton == ButtonState.Released &&
								ms.Position.X > m_pocketCardsX[i] && ms.Position.Y > defaultScreenY &&
								ms.Position.X < m_pocketCardsX[i] + m_pocketCardWidth && !keyExpand)
							{
								m_isDraggingPocketCards[i] = true;
								m_pocketCardDragOffset = ms.Position.Y - defaultScreenY;
							}
							else
							{
								m_pocketCardsY[i] += (defaultY - m_pocketCardsY[i]) * dt * 10;
							}
						}
					}
					
					// ** Updates the reveal animation for community cards **
					if (!playerChipsMoving)
					{
						if (m_communityCardsRevealed < m_connection.CommunityCardsRevealed)
						{
							m_communityCardRevealProgress += dt * DEAL_ANIMATION_SPEED * 0.5f;
							if (m_communityCardRevealProgress >= 1.0f)
							{
								m_communityCardsRevealed++;
								m_communityCardRevealProgress = 0;
							}
						}
						else
						{
							showActionsUI = currentClient != null && m_connection.SelfClientId == currentClient.ClientId;
						}
					}
				}
			}
			
			bool showHandSummary = false;
			
			if (revealPocketCards && !playerChipsMoving &&
			    m_communityCardsRevealed == m_connection.CommunityCardsRevealed)
			{
				const float REVEAL_SPEED = DEAL_ANIMATION_SPEED * 0.4f;
				m_pocketCardRevealProgress += dt * REVEAL_SPEED;
				showActionsUI = false;
				
				if (m_pocketCardRevealProgress > 1.0f)
				{
					m_pocketCardRevealProgress = 1.0f;
					showHandSummary = true;
				}
			}
			
			if (showHandSummary && !m_waitingForPlayers && m_winner == null)
			{
				m_handSummaryRevealProgress += dt;
				
				if (m_handSummaryRevealProgress > HAND_SUMMARY_REVEAL_DELAY)
				{
					if (!m_endSummary.Visible)
						m_endSummary.Show(m_connection);
					m_handSummaryRevealProgress = HAND_SUMMARY_REVEAL_DELAY;
				}
				
				m_blurIntensity = m_handSummaryRevealProgress / HAND_SUMMARY_REVEAL_DELAY;
			}
			else
			{
				if (m_waitingForPlayers || m_winner != null)
					UI.AnimateInc(ref m_blurIntensity, dt);
				else
					UI.AnimateDec(ref m_blurIntensity, dt);
			}
			
			if (showActionsUI)
				UI.AnimateInc(ref m_actionButtonsAlpha, dt);
			else
				UI.AnimateDec(ref m_actionButtonsAlpha, dt);
			
			//Updates the call button
			bool callButtonHovered = showActionsUI && m_callButtonRectangle.Contains(ms.Position);
			if (callButtonHovered)
				UI.AnimateInc(ref m_callButtonHighlight, dt);
			else
				UI.AnimateDec(ref m_callButtonHighlight, dt);
			
			if ((callButtonHovered && ms.LeftButton == ButtonState.Pressed && m_prevMS.LeftButton == ButtonState.Released) ||
				(showActionsUI && ks.IsKeyDown(Keys.Return) && !m_prevKS.IsKeyDown(Keys.Return)))
			{
				if (m_callAmount + m_raiseAmount >= m_players[m_selfPlayerIndex].Player.Chips)
					m_connection.AllIn();
				else if (m_raiseAmount > 0)
					m_connection.Raise(m_raiseAmount);
				else
					m_connection.Call();
				m_raiseAmount = 0;
			}
			
			//Updates the fold button
			if (showActionsUI && m_foldButtonRectangle.Contains(ms.Position))
			{
				UI.AnimateInc(ref m_foldButtonHighlight, dt);
				if (ms.LeftButton == ButtonState.Pressed && m_prevMS.LeftButton == ButtonState.Released)
				{
					m_connection.Fold();
				}
			}
			else
				UI.AnimateDec(ref m_foldButtonHighlight, dt);
			
			if (m_betButtonCooldown > 0)
			{
				if (ms.LeftButton == ButtonState.Pressed || ks.IsKeyDown(Keys.Up) || ks.IsKeyDown(Keys.Down))
					m_betButtonCooldown = Math.Max(m_betButtonCooldown - dt, 0);
				else
					m_betButtonCooldown = 0;
			}
			
			int selfChips = m_players[m_selfPlayerIndex].Player.Chips;
			const float BET_BUTTON_COOLDOWN = 0.1f;
			
			// ** Bet up button **
			
			//Updates disable animation
			bool betUpEnabled = m_raiseAmount + m_callAmount < selfChips;
			if (betUpEnabled)
				UI.AnimateDec(ref m_betUpButtonDisProgress, dt);
			else
				UI.AnimateInc(ref m_betUpButtonDisProgress, dt);
			
			//Updates hover animation
			bool betUpHovered = m_betUpButtonRectangle.Contains(ms.Position);
			if (showActionsUI && betUpEnabled && betUpHovered)
				UI.AnimateInc(ref m_betUpButtonHighlight, dt);
			else
				UI.AnimateDec(ref m_betUpButtonHighlight, dt);
			
			//Checks for click events
			if (showActionsUI && betUpEnabled && m_betButtonCooldown < 1E-6f &&
				(betUpHovered && ms.LeftButton == ButtonState.Pressed || ks.IsKeyDown(Keys.Up)))
			{
				if (ks.IsKeyDown(Keys.LeftShift))
					m_raiseAmount = Math.Max(selfChips - m_callAmount, 0);
				else
					m_raiseAmount = m_raiseAmount == 0 ? m_connection.MinimumRaise : m_raiseAmount + 1;
				
				m_betButtonCooldown = BET_BUTTON_COOLDOWN;
				if (ms.LeftButton == ButtonState.Pressed && m_prevMS.LeftButton == ButtonState.Released ||
					ks.IsKeyDown(Keys.Up) && !m_prevKS.IsKeyDown(Keys.Up))
				{
					m_betButtonCooldown *= 3;
				}
			}
			
			// ** Bet down button **
			
			//Updates disable animation
			bool betDownEnabled = m_raiseAmount > 0;
			if (betDownEnabled)
				UI.AnimateDec(ref m_betDownButtonDisProgress, dt);
			else
				UI.AnimateInc(ref m_betDownButtonDisProgress, dt);
			
			//Updates hover animation
			bool betDownHovered = m_betDownButtonRectangle.Contains(ms.Position);
			if (showActionsUI && betDownEnabled && betDownHovered)
				UI.AnimateInc(ref m_betDownButtonHighlight, dt);
			else
				UI.AnimateDec(ref m_betDownButtonHighlight, dt);
			
			//Checks for click events
			if (showActionsUI && betDownEnabled && m_betButtonCooldown < 1E-6f &&
				(betDownHovered && ms.LeftButton == ButtonState.Pressed || ks.IsKeyDown(Keys.Down)))
			{
				if (ks.IsKeyDown(Keys.LeftShift))
					m_raiseAmount = 0;
				else
					m_raiseAmount = m_raiseAmount == m_connection.MinimumRaise ? 0 : m_raiseAmount - 1;
				
				m_betButtonCooldown = BET_BUTTON_COOLDOWN;
				if (ms.LeftButton == ButtonState.Pressed && m_prevMS.LeftButton == ButtonState.Released ||
					ks.IsKeyDown(Keys.Down) && !m_prevKS.IsKeyDown(Keys.Down))
				{
					m_betButtonCooldown *= 3;
				}
			}
			
			const float ZOOM_SPEED = 0.5f;
			if (ks.IsKeyDown(Keys.Z))
				UI.AnimateInc(ref m_communityCardFocusProgress, dt, ZOOM_SPEED);
			else
				UI.AnimateDec(ref m_communityCardFocusProgress, dt, ZOOM_SPEED);
			
			m_prevMS = ms;
			m_prevKS = ks;
		}
		
		public override void OnResize(int newWidth, int newHeight)
		{
			m_endSummary.OnResize(newWidth, newHeight);
			m_totalsPane.OnResize(newWidth, newHeight);
			
			m_displayWidth = newWidth;
			m_displayHeight = newHeight;
			
			m_logAreaHeight = (int)(newHeight * 0.25);
			
			float aspectRatio = (float)newWidth / newHeight;
			m_projectionMatrix = Graphics.CreateProjectionMatrix(FOV, aspectRatio, Z_NEAR, Z_FAR);
			
			const float POCKET_CARD_SPACING = 0.05f;
			const float POCKET_CARD_WIDTH = 0.15f;
			
			m_pocketCardWidth = newWidth * POCKET_CARD_WIDTH;
			m_pocketCardHeight = m_pocketCardWidth * Assets.CardsTexture.CardHeight / Assets.CardsTexture.CardWidth;
			m_pocketCardsX[0] = newWidth * (0.5f - POCKET_CARD_WIDTH - POCKET_CARD_SPACING * 0.5f);
			m_pocketCardsX[1] = newWidth * (0.5f + POCKET_CARD_SPACING * 0.5f);
			
			const float KEYBOARD_GUIDE_PADDING_X = 50 * UI.SCALE;
			const float KEYBOARD_GUIDE_HEIGHT_PERCENTAGE = 0.15f;
			float kbGuideHeight = newHeight * KEYBOARD_GUIDE_HEIGHT_PERCENTAGE;
			float kbGuideWidth = m_keyboardGuideTexture.Width * kbGuideHeight / m_keyboardGuideTexture.Height;
			
			m_keyboardGuideRectangle = new RectangleF(newWidth - KEYBOARD_GUIDE_PADDING_X - kbGuideWidth,
			                                          (newHeight - kbGuideHeight) / 2, kbGuideWidth, kbGuideHeight);
			
			float rtmWidth = Assets.SmallButton2Texture.Width * UI.SCALE;
			float rtmHeight = Assets.SmallButton2Texture.Height * UI.SCALE;
			m_returnToMenuButtonRect = new RectangleF((newWidth - rtmWidth) / 2, m_displayHeight * 0.55f, rtmWidth, rtmHeight);
			
			LayoutActionsUI();
		}
		
		private void LayoutActionsUI()
		{
			float centerX = m_displayWidth / 2.0f;
			
			float actionCenterY = 80;
			float buttonCenterOffset = 40;
			float buttonWidth = Assets.SmallButtonTexture.Width * UI.SCALE;
			float buttonHeight = Assets.SmallButtonTexture.Height * UI.SCALE;
			float buttonY = (actionCenterY - Assets.ButtonTexture.Height / 2.0f) * UI.SCALE;
			
			m_callButtonRectangle = new RectangleF(centerX - (buttonCenterOffset + Assets.SmallButtonTexture.Width) *
												   UI.SCALE, buttonY, buttonWidth, buttonHeight);
			m_foldButtonRectangle = new RectangleF(centerX + buttonCenterOffset * UI.SCALE,
												   buttonY, buttonWidth, buttonHeight);
			
			float betArrowsOffsetY = Assets.SmallButtonTexture.Height / 2.0f;
			float betArrowX = centerX - Assets.ArrowButtonTexture.Width * UI.SCALE * 0.5f;
			float betArrowWidth = Assets.ArrowButtonTexture.Width * UI.SCALE;
			float betArrowHeight = Assets.ArrowButtonTexture.Height * UI.SCALE;
			float betUpY = (actionCenterY - betArrowsOffsetY - Assets.ArrowButtonTexture.Height) * UI.SCALE;
			float betDownY = (actionCenterY + betArrowsOffsetY) * UI.SCALE;
			m_betUpButtonRectangle = new RectangleF(betArrowX, betUpY, betArrowWidth, betArrowHeight);
			m_betDownButtonRectangle = new RectangleF(betArrowX, betDownY, betArrowWidth, betArrowHeight);
			
			m_callAmountPosition = new Vector2(centerX, actionCenterY * UI.SCALE);
		}
		
		private static float CardSlideStep(float t)
		{
			float x = 1 - Utils.Clamp(t, 0, 1);
			return 1.0f - x * x;
		}
		
		private static Vector3 InterpolateChipPosition(Vector3 pos1, Vector3 pos2, float t)
		{
			t = Utils.SmoothStep(t);
			
			const float PARABOLA_HEIGHT = 0.1f;
			
			float a = t - 0.5f;
			float yOffset = PARABOLA_HEIGHT * (1 - 4 * a * a);
			
			return Vector3.Lerp(pos1, pos2, t) + new Vector3(0, yOffset, 0);
		}
		
		private void PrepareChipsRenderer(ChipsRenderer chipsRenderer)
		{
			chipsRenderer.Begin();
			
			const float STACK_ROW_STRIDE = ChipsRenderer.CHIP_SCALE * 2.2f;
			
			foreach (PlayerEntry player in m_players)
			{
				Random rand = new Random(player.Client.ClientId);
				
				Vector3 GetChipOffset(int index, int yDir)
				{
					const int STACK_ROW_SIZE = 4;
					
					int stackIndex = index / STACK_SIZE;
					int stackX = stackIndex % STACK_ROW_SIZE;
					int stackY = stackIndex / STACK_ROW_SIZE;
					
					int entropyIndex = (index * 2) % m_chipEntropy.Length;
					
					return new Vector3(
						   (m_chipEntropy[entropyIndex] - 0.5f) * 0.01f,
						   ChipsRenderer.CHIP_HEIGHT * (index % STACK_SIZE),
						   (m_chipEntropy[entropyIndex + 1] - 0.5f) * 0.01f) +
						   player.BoardAreaLeft * -(stackX * STACK_ROW_STRIDE) +
						   player.BoardAreaUp * (stackY * yDir * STACK_ROW_STRIDE);
				}
				
				Vector3 stackBasePos = player.BoardAreaCenter - player.BoardAreaUp * 0.15f;
				Vector3 potBasePos = stackBasePos + player.BoardAreaUp * 0.75f;
				
				//Flipped loop gives better early Z rejection
				int inStackSize = player.InStackRotations.Count - player.NumMovingChips;
				for (int j = inStackSize - 1; j >= 0; j--)
				{
					chipsRenderer.Add(stackBasePos + GetChipOffset(j, 1), player.InStackRotations[j]);
				}
				
				for (int j = player.OutStackRotations.Count - 1; j >= 0; j--)
				{
					chipsRenderer.Add(potBasePos + GetChipOffset(j, -1), player.OutStackRotations[j]);
				}
				
				//If a chip is not being moved to the pot, skip the last section, which draws the chip at it's current position.
				if (player.ChipsMoveProgress < 1E-6f)
					continue;
				
				for (int i = 0; i < player.NumMovingChips; i++)
				{
					int sourceIndex = inStackSize + i;
					int dstIndex = player.OutStackRotations.Count + i;
					
					Vector3 sourcePos = stackBasePos + GetChipOffset(sourceIndex, 1);
					Vector3 destPos = potBasePos + GetChipOffset(dstIndex, -1);
					Vector3 pos = InterpolateChipPosition(sourcePos, destPos, player.ChipsMoveProgress);
					chipsRenderer.Add(pos, player.InStackRotations[sourceIndex]);
				}
			}
			
			chipsRenderer.End();
		}
		
		private void PrepareCardsRenderer(CardRenderer cardRenderer)
		{
			const float REVEAL_TOP_Y = 0.2f;
			
			cardRenderer.Reset();
			
			//Adds pocket cards
			Quaternion cardSourceRotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, MathF.PI / 2);
			Vector3 cardSourcePos = CARD_DECK_POSITION + new Vector3(0, CARD_DECK_HEIGHT, 0);
			for (int i = 0; i < m_players.Length; i++)
			{
				Matrix4x4 cardRotationMatrix = new Matrix4x4(
						m_players[i].BoardAreaUp.X, m_players[i].BoardAreaUp.Y, m_players[i].BoardAreaUp.Z, 0,
						0, 1, 0, 0,
						m_players[i].BoardAreaLeft.X, m_players[i].BoardAreaLeft.Y, m_players[i].BoardAreaLeft.Z, 0,
						0, 0, 0, 0);
				Quaternion cardRotation = Quaternion.CreateFromRotationMatrix(cardRotationMatrix);
				
				float dealProgress = m_initialDealProgress - i * 2;
				for (int c = 0; c < 2; c++)
				{
					float cardDealProgress = CardSlideStep(dealProgress - c);
					
					Vector3 destinationPos = m_players[i].BoardAreaCenter;
					destinationPos.Y += 0.005f + 0.001f * (c + 1);
					destinationPos += m_players[i].BoardAreaLeft * ((c - 0.5f) * 0.25f + 0.4f);
					
					Quaternion dstRotation = cardRotation *
						Quaternion.CreateFromAxisAngle(Vector3.UnitY, m_players[i].CardRotations[c]);
					
					const float REV_DELTA_ROT = 0.1f;
					
					if (m_players[i].Client.RevealedPocketCards != null)
					{
						Quaternion revRotation = cardRotation * 
							Quaternion.CreateFromAxisAngle(Vector3.UnitX, MathF.PI) * 
							Quaternion.CreateFromAxisAngle(Vector3.UnitY, MathF.PI / 2 + REV_DELTA_ROT * (c * 2 - 1));
						Quaternion rotation = Quaternion.Slerp(dstRotation, revRotation, m_pocketCardRevealProgress);
						
						destinationPos.Y += MathF.Sin(MathF.PI * m_pocketCardRevealProgress) * REVEAL_TOP_Y;
						
						//Only cast shadows if the card is playing the flip animation
						bool castShadows = m_pocketCardRevealProgress < 0.99f;
						
						Card card = m_players[i].Client.RevealedPocketCards[c];
						cardRenderer.Add(destinationPos, rotation, card, castShadows);
					}
					else
					{
						Vector3 position = Vector3.Lerp(cardSourcePos, destinationPos, cardDealProgress);
						Quaternion rotation = Quaternion.Slerp(cardSourceRotation, dstRotation, cardDealProgress);
						
						//Only cast shadows if the card is playing the deal animation
						bool castShadows = cardDealProgress < 0.99f;
						
						cardRenderer.AddUnknown(position, rotation, castShadows);
					}
				}
			}
			
			//Adds community cards
			for (int i = 0; i < 5; i++)
			{
				//Distance between the center of two community cards
				const float STRIDE = 0.4f;
				
				float dealProgress = CardSlideStep(m_initialDealProgress - (m_players.Length * 2 + i));
				
				Vector3 destinationPos = new Vector3(0, 0.105f, STRIDE * 2 - i * STRIDE);
				Vector3 position = Vector3.Lerp(cardSourcePos, destinationPos, dealProgress);
				bool castShadows = false;
				
				Quaternion cardRotation = cardSourceRotation;
				if (dealProgress < 0.99f)
				{
					//The card is playing the deal animation, so it should cast shadows
					castShadows = true;
				}
				else
				{
					if (m_communityCardsRevealed > i)
					{
						//The reveal animation is complete, so flip the card fully
						cardRotation *= Quaternion.CreateFromAxisAngle(Vector3.UnitZ, MathF.PI);
					}
					else if (m_communityCardsRevealed == i)
					{
						//The card is being revealed, so play the reveal animation.
						
						position.Y += MathF.Sin(MathF.PI * m_communityCardRevealProgress) * REVEAL_TOP_Y;
						castShadows = true;
						
						float rotationProgress = Utils.Clamp((m_communityCardRevealProgress - 0.5f) * 2 + 0.5f, 0, 1);
						cardRotation *= Quaternion.CreateFromAxisAngle(Vector3.UnitZ, MathF.PI * rotationProgress);
					}
				}
				
				cardRenderer.Add(position, cardRotation, m_connection.CommunityCards[i], castShadows);
			}
		}
		
		public override void Draw(DrawArgs drawArgs)
		{
			// ** Updates the camera transform **
			const float CC_FOCUS_CAMERA_DIST = 1;
			const float CC_FOCUS_CAMERA_PITCH = MathF.PI * 0.4f;
			
			float communityCardFocusCameraYaw = MathF.PI / 2.0f;
			if (Utils.NMod(m_cameraYaw, MathF.PI * 2) > MathF.PI)
				communityCardFocusCameraYaw = -communityCardFocusCameraYaw;
			
			Quaternion focusRotation = Quaternion.CreateFromAxisAngle(Vector3.UnitX, CC_FOCUS_CAMERA_PITCH) *
			                           Quaternion.CreateFromAxisAngle(Vector3.UnitY, communityCardFocusCameraYaw);
			Quaternion defaultRotation = Quaternion.CreateFromAxisAngle(Vector3.UnitX, m_cameraPitch) *
			                             Quaternion.CreateFromAxisAngle(Vector3.UnitY, m_cameraYaw);
			
			float focusProgress = Utils.SmoothStep(m_communityCardFocusProgress);
			Quaternion cameraRotation = Quaternion.Slerp(defaultRotation, focusRotation, focusProgress);
			float cameraDist = Utils.Lerp(CAMERA_DIST, CC_FOCUS_CAMERA_DIST, focusProgress);
			Vector3 cameraPos = Vector3.Transform(new Vector3(0, 0, cameraDist), Quaternion.Inverse(cameraRotation));
			
			m_viewMatrix = Matrix4x4.CreateFromQuaternion(cameraRotation) *
			               Matrix4x4.CreateTranslation(0, 0, -cameraDist);
			
			
			PrepareChipsRenderer(ChipsRenderer.Instance);
			PrepareCardsRenderer(CardRenderer.Instance);
			
			Graphics.SetFixedFunctionState(FFState.DepthTest | FFState.DepthWrite);
			m_shadowMapper.RenderShadows(() =>
			{
				ChipsRenderer.Instance.DrawShadows();
				CardRenderer.Instance.DrawShadow();
				BoardModel.Instance.DrawShadow();
			});
			
			var blurEffect = BlurEffect.Instance;
			
			blurEffect.BindInputFramebuffer();
			
			Graphics.DiscardColor();
			Graphics.ClearDepth();
			Graphics.SetFixedFunctionState(FFState.Multisample | FFState.DepthWrite | FFState.DepthTest);
			
			Matrix4x4 viewProj = m_viewMatrix * m_projectionMatrix;
			m_viewProjUniformBuffer.Update(ref viewProj, cameraPos);
			m_viewProjUniformBuffer.Bind(0);
			
			m_shadowMapper.Bind();
			
			// ** Opaque geometry **
			
			BoardModel.Instance.DrawNormal();
			
			ChipsRenderer.Instance.Draw();
			
			SkyRenderer.Draw();
			
			// ** Alpha blended geometry **
			
			CardRenderer.Instance.Draw();
			
			m_playerNameRenderer.Draw(m_players.Select(player => player.TextColor));
			
			blurEffect.RenderBlur(m_blurIntensity);
			
			// ** UI **
			
			drawArgs.SpriteBatch.Begin();
			
			//Draws the pocket cards UI
			if (m_connection.HasDealtPocketCards)
			{
				for (int i = 0; i < 2; i++)
				{
					RectangleF rect = new RectangleF(m_pocketCardsX[i], m_displayHeight - m_pocketCardsY[i],
													 m_pocketCardWidth, m_pocketCardHeight);
					RectangleF srcRect = Assets.CardsTexture.GetSourceRectangle(m_connection.PocketCards[i]);
					drawArgs.SpriteBatch.Draw(Assets.CardsTexture.Texture, rect, srcRect, Color.White);
				}
			}
			
			//Draws the action buttons
			if (m_actionButtonsAlpha > 0)
			{
				const float OFFSET_DIST = 5;
				
				//Draws the call button
				float callButtonOffset = m_callButtonHighlight * OFFSET_DIST;
				Color callButtonColor = Color.Lerp(UI.DEFAULT_BUTTON_COLOR, UI.HOVERED_BUTTON_COLOR, m_callButtonHighlight);
				RectangleF callButtonRect = m_callButtonRectangle;
				callButtonRect.X -= callButtonOffset;
				drawArgs.SpriteBatch.Draw(Assets.SmallButtonTexture, callButtonRect,
					callButtonColor.ScaleAlpha(m_actionButtonsAlpha), SpriteEffects.FlipH);
				
				Color textColor = new Color(1, 1, 1, m_actionButtonsAlpha);
				
				const float TEXT_HEIGHT_PERCENTAGE = 0.6f;
				
				int callAmount = m_callAmount + m_raiseAmount;
				bool allIn = callAmount >= m_players[m_selfPlayerIndex].Player.Chips;
				if (allIn)
					callAmount = m_players[m_selfPlayerIndex].Player.Chips;
				
				//Draws the call button label
				string callText = allIn ? "ALL IN" : (m_raiseAmount > 0 ? "RAISE" : (m_callAmount > 0 ? "CALL" : "CHECK"));
				Vector2 callTextSize = Assets.RegularFont.MeasureString(callText);
				float callTextScale = TEXT_HEIGHT_PERCENTAGE * m_callButtonRectangle.Height / callTextSize.Y;
				Vector2 callTextPos = m_callButtonRectangle.Center() - callTextSize * callTextScale * 0.5f;
				callTextPos.X -= callButtonOffset;
				drawArgs.SpriteBatch.DrawString(Assets.RegularFont, callText, callTextPos, textColor, callTextScale);
				
				//Draws the fold button
				float foldButtonOffset = m_foldButtonHighlight * OFFSET_DIST;
				Color foldButtonColor = Color.Lerp(UI.DEFAULT_BUTTON_COLOR, UI.HOVERED_BUTTON_COLOR, m_foldButtonHighlight);
				RectangleF foldButtonRect = m_foldButtonRectangle;
				foldButtonRect.X += foldButtonOffset;
				drawArgs.SpriteBatch.Draw(Assets.SmallButtonTexture, foldButtonRect,
					foldButtonColor.ScaleAlpha(m_actionButtonsAlpha));
				
				//Draws the fold button label
				string foldText = "FOLD";
				Vector2 foldTextSize = Assets.RegularFont.MeasureString(foldText);
				float foldTextScale = TEXT_HEIGHT_PERCENTAGE * m_foldButtonRectangle.Height / foldTextSize.Y;
				Vector2 foldTextPos = m_foldButtonRectangle.Center() - foldTextSize * foldTextScale * 0.5f;
				foldTextPos.X += foldButtonOffset;
				drawArgs.SpriteBatch.DrawString(Assets.RegularFont, foldText, foldTextPos, textColor, foldTextScale);
				
				//Draws the bet up arrow
				Color betUpColor = Color.Lerp(UI.DEFAULT_BUTTON_COLOR, UI.HOVERED_BUTTON_COLOR, m_betUpButtonHighlight);
				betUpColor = Color.Lerp(betUpColor, UI.DISABLED_BUTTON_COLOR, m_betUpButtonDisProgress);
				RectangleF betUpButtonRect = m_betUpButtonRectangle;
				betUpButtonRect.Y -= m_betUpButtonHighlight * OFFSET_DIST * 0.5f;
				drawArgs.SpriteBatch.Draw(Assets.ArrowButtonTexture, betUpButtonRect,
					betUpColor.ScaleAlpha(m_actionButtonsAlpha));
				
				//Draws the bet down arrow
				Color betDownColor = Color.Lerp(UI.DEFAULT_BUTTON_COLOR, UI.HOVERED_BUTTON_COLOR, m_betDownButtonHighlight);
				betDownColor = Color.Lerp(betDownColor, UI.DISABLED_BUTTON_COLOR, m_betDownButtonDisProgress);
				RectangleF betDownButtonRect = m_betDownButtonRectangle;
				betDownButtonRect.Y += m_betDownButtonHighlight * OFFSET_DIST * 0.5f;
				drawArgs.SpriteBatch.Draw(Assets.ArrowButtonTexture, betDownButtonRect,
					betDownColor.ScaleAlpha(m_actionButtonsAlpha), SpriteEffects.FlipV);
				
				string callAmountString = callAmount.ToString();
				Vector2 callAmountTextSize = Assets.RegularFont.MeasureString(callAmountString);
				float callAmountTextScale = TEXT_HEIGHT_PERCENTAGE * m_foldButtonRectangle.Height / callAmountTextSize.Y;
				Vector2 callAmountTextPos = m_callAmountPosition - callAmountTextSize * callAmountTextScale * 0.5f;
				drawArgs.SpriteBatch.DrawString(Assets.RegularFont, callAmountString, callAmountTextPos, Color.White, callAmountTextScale);
			}
			
			float kbGuideAlpha = Math.Max(0.8f * (1.0f - m_blurIntensity), 0.0f);
			drawArgs.SpriteBatch.Draw(m_keyboardGuideTexture, m_keyboardGuideRectangle, new Color(1, 1, 1, kbGuideAlpha));
			
#if DEBUG
			//Draws the FPS counter
			string fpsText = "FPS: " + Math.Round(1.0 / drawArgs.DeltaTime);
			drawArgs.SpriteBatch.DrawString(Assets.RegularFont, fpsText, new Vector2(5, 5),
			                                new Color(255, 255, 255, 150), 0.2f);
#endif
			
			m_totalsPane.Draw(drawArgs.SpriteBatch, m_connection);
			
			if (m_waitingForPlayers)
			{
				const string WAITING_FOR_PLAYERS_TEXT = "Waiting for Players...";
				float textWidth = m_displayWidth * 0.5f;
				
				Vector2 size = Assets.RegularFont.MeasureString(WAITING_FOR_PLAYERS_TEXT);
				float scale = textWidth / size.X;
				size *= scale;
				
				drawArgs.SpriteBatch.DrawString(Assets.RegularFont, WAITING_FOR_PLAYERS_TEXT,
				                                new Vector2(m_displayWidth - size.X, m_displayHeight - size.Y) / 2,
				                                Color.White, scale);
			}
			
			if (m_winner != null)
			{
				float winnerTextWidth = 0.4f * m_displayWidth;
				string winnerText = m_winner.Name + " won the game";
				
				Vector2 winnerTextSize = Assets.RegularFont.MeasureString(winnerText);
				float winnerTextScale = winnerTextWidth / winnerTextSize.X;
				winnerTextSize *= winnerTextScale;
				
				Vector2 winnerTextPos = new Vector2((m_displayWidth - winnerTextSize.X) / 2.0f,
				                                    m_displayHeight / 2.0f - winnerTextSize.Y);
				
				drawArgs.SpriteBatch.DrawString(Assets.RegularFont, winnerText, winnerTextPos, Color.White,
				                                winnerTextScale);
				
				drawArgs.SpriteBatch.Draw(Assets.SmallButton2Texture, m_returnToMenuButtonRect,
					Color.Lerp(UI.DEFAULT_BUTTON_COLOR, UI.HOVERED_BUTTON_COLOR, m_returnToMenuButtonHighlight));
				
				const string RETURN_TO_MENU_STRING = "MAIN MENU";
				float textHeight = UI.TEXT_HEIGHT_PERCENTAGE * m_returnToMenuButtonRect.Height;
				Vector2 textSize = Assets.RegularFont.MeasureString(RETURN_TO_MENU_STRING);
				float textScale = textHeight / textSize.Y;
				textSize *= textScale;
				
				drawArgs.SpriteBatch.DrawString(Assets.RegularFont, RETURN_TO_MENU_STRING, 
				                                m_returnToMenuButtonRect.Center() - textSize / 2, Color.White, textScale);
			}
			
			Graphics.SetFixedFunctionState(FFState.AlphaBlend);
			drawArgs.SpriteBatch.End();
			
			m_endSummary.Draw();
			
			if (m_log.Count == 0)
				return;
			
			drawArgs.SpriteBatch.Begin();
			
			const float PADDING = 50 * UI.SCALE;
			
			//Draws the message log
			for (int i = 0; i < m_log.Count; i++)
			{
				const float DEF_ALPHA = 0.9f;
				
				LogMessage message = m_log[m_log.Count - i - 1];
				
				float textY = m_displayHeight - PADDING - LOG_LINE_HEIGHT * (i + m_logFadeInProgress) + m_logScrollY;
				
				float textScale = LOG_LINE_HEIGHT / Assets.RegularFont.LineHeight;
				Vector2 sourceSize = Assets.BoldFont.MeasureString(message.Source) * textScale;
				
				float alpha = i == 0 ? m_logFadeInProgress * DEF_ALPHA : DEF_ALPHA;
				Color sourceColor = new Color(0.6f, 0.6f, 1.0f, alpha);
				Color messageColor = new Color(1.0f, 1.0f, 1.0f, alpha);
				
				drawArgs.SpriteBatch.DrawString(Assets.BoldFont, message.Source, new Vector2(PADDING, textY), sourceColor, textScale);
				drawArgs.SpriteBatch.DrawString(Assets.RegularFont, message.Message, new Vector2(PADDING + sourceSize.X, textY), messageColor, textScale);
			}
			
			float totalHeight = LogHeight;
			if (totalHeight > m_logAreaHeight)
			{
				float scrollBarH = m_logAreaHeight * m_logAreaHeight / totalHeight;
				float scrollBarY = m_logScrollY / totalHeight * m_logAreaHeight;
				
				var scrollRect = new RectangleF(0, m_displayHeight - scrollBarY - scrollBarH - PADDING, 3, scrollBarH);
				drawArgs.SpriteBatch.Draw(Assets.PixelTexture, scrollRect, Color.White);
			}
			
			int scissorH = m_logAreaHeight + (int)PADDING;
			Graphics.SetScissorRectangle(0, m_displayHeight - scissorH, m_displayWidth, scissorH);
			Graphics.SetFixedFunctionState(FFState.AlphaBlend | FFState.ScissorTest);
			drawArgs.SpriteBatch.End();
		}
		
		public void Dispose()
		{
			m_shadowMapper.Dispose();
			m_playerNameRenderer.Dispose();
			m_viewProjUniformBuffer.Dispose();
			m_keyboardGuideTexture.Dispose();
			m_endSummary.Dispose();
		}
	}
}
