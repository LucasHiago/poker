namespace Poker
{
	public abstract class GameState
	{
		public abstract void Update(float dt);
		public abstract void Draw(DrawArgs drawArgs);
		public virtual void OnTextInput(string text) { }
		public virtual void OnKeyPress(Keys key) { }
		public virtual void OnResize(int newWidth, int newHeight) { }
		public virtual void Activated() { }
	}
	
	public struct DrawArgs
	{
		public float DeltaTime;
		public SpriteBatch SpriteBatch;
	}
}
