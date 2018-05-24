namespace Poker.Net.Server
{
	public partial class Server
	{
		private class SelfClient : BaseClient
		{
			public override bool IsConnected => true;
			
			public SelfClient(string name)
				: base(0, name)
			{
				
			}
		}
	}
}
