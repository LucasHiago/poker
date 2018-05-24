using Poker.GLTF;

namespace Poker
{
	public static class Assets
	{
		public static Texture2D ButtonTexture { get; private set; }
		public static Texture2D SmallButtonTexture { get; private set; }
		public static Texture2D SmallButton2Texture { get; private set; }
		public static Texture2D ArrowButtonTexture { get; private set; }
		public static Texture2D TextBoxBackTexture { get; private set; }
		public static Texture2D TextBoxInnerTexture { get; private set; }
		public static Texture2D PixelTexture { get; private set; }
		
		public static Texture2D MiniBackTexture { get; private set; }
		public static Texture2D MiniClubsTexture { get; private set; }
		public static Texture2D MiniDiamondsTexture { get; private set; }
		public static Texture2D MiniHeartsTexture { get; private set; }
		public static Texture2D MiniSpadesTexture { get; private set; }
		
		public static Texture2D CardBackTexture { get; private set; }
		public static CardsTexture CardsTexture { get; private set; }
		
		public static SpriteFont RegularFont { get; private set; }
		public static SpriteFont BoldFont { get; private set; }
		
		public static GLTF.Model BoardModel { get; private set; }
		
		public static void Load()
		{
			CardsTexture        = new CardsTexture();
			CardBackTexture     = Texture2D.Load("Textures/CardBack.png", Texture2D.Type.sRGB32);
			ButtonTexture       = Texture2D.Load("UI/Button.png");
			SmallButtonTexture  = Texture2D.Load("UI/ButtonSmall.png");
			SmallButton2Texture = Texture2D.Load("UI/ButtonSmall2.png");
			ArrowButtonTexture  = Texture2D.Load("UI/ArrowButton.png");
			TextBoxBackTexture  = Texture2D.Load("UI/TextBoxBack.png");
			TextBoxInnerTexture = Texture2D.Load("UI/TextBoxInner.png");
			PixelTexture        = Texture2D.Load("UI/Pixel.png");
			
			MiniBackTexture     = Texture2D.Load("Textures/MiniBack.png");
			MiniClubsTexture    = Texture2D.Load("Textures/MiniClubs.png");
			MiniDiamondsTexture = Texture2D.Load("Textures/MiniDiamonds.png");
			MiniHeartsTexture   = Texture2D.Load("Textures/MiniHearts.png");
			MiniSpadesTexture   = Texture2D.Load("Textures/MiniSpades.png");
			
			RegularFont         = new SpriteFont(Program.EXEDirectory + "/Res/UI/Font.fnt");
			BoldFont            = new SpriteFont(Program.EXEDirectory + "/Res/UI/FontBold.fnt");
			BoardModel          = GLTFImporter.Import(Program.EXEDirectory + "/Res/Models/Board.gltf");
		}
		
		public static void Unload()
		{
			CardsTexture.Dispose();
			ButtonTexture.Dispose();
			SmallButtonTexture.Dispose();
			SmallButton2Texture.Dispose();
			ArrowButtonTexture.Dispose();
			CardBackTexture.Dispose();
			TextBoxBackTexture.Dispose();
			TextBoxInnerTexture.Dispose();
			MiniBackTexture.Dispose();
			MiniClubsTexture.Dispose();
			MiniDiamondsTexture.Dispose();
			MiniHeartsTexture.Dispose();
			MiniSpadesTexture.Dispose();
			PixelTexture.Dispose();
			BoardModel.Dispose();
			RegularFont.Dispose();
			BoldFont.Dispose();
		}
	}
}
