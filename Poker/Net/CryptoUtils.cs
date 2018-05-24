namespace Poker.Net
{
	public static class CryptoUtils
	{
		public static ulong ExpMod(ulong x, ulong e, ulong mod)
		{
			if (e == 1)
				return x % mod;
			
			ulong val = ExpMod(x, e / 2, mod);
			val = val * val % mod;
			
			if (e % 2 == 1)
				val = val * (x % mod) % mod;
			
			return val;
		}
		
		public static ulong MInverse(ulong a, ulong mod)
		{
			//TODO: Change to faster algorithm
			
			for (ulong i = 1; i < mod - 1; i++)
			{
				if ((a * i) % mod == 1)
					return i;
			}
			return 0;
		}
	}
}
