using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;

namespace Poker
{
	public class SpriteFont : IDisposable
	{
		public class InvalidSpriteFontException : Exception
		{
			public InvalidSpriteFontException(string message)
				: base(message) { }
		}
		
		public struct Character
		{
			public int TextureX;
			public int TextureY;
			public int Width;
			public int Height;
			public int XOffset;
			public int YOffset;
			public int XAdvance;
		}
		
		private readonly Dictionary<char, Character> m_characters = new Dictionary<char, Character>();
		
		private struct KerningPair : IComparable<KerningPair>
		{
			public char First;
			public char Second;
			public int Amount;
			
			public int CompareTo(KerningPair other)
			{
				int firstComp = First.CompareTo(other.First);
				return firstComp != 0 ? firstComp : Second.CompareTo(other.Second);
			}
		}
		
		private readonly List<KerningPair> m_kerningPairs = new List<KerningPair>();
		
		public readonly Texture2D Texture;
		
		public readonly int LineHeight;
		
		public SpriteFont(string path)
		{
			using (StreamReader reader = new StreamReader(File.OpenRead(path)))
			{
				string imageFileName = null;
				int imageWidth = 0;
				int imageHeight = 0;
				
				while (!reader.EndOfStream)
				{
					string line = reader.ReadLine();
					if (line == null)
						break;
					
					string[] parts = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
					if (parts.Length == 0)
						continue;
					
					int GetPartValueI(string partName)
					{
						string part = parts.FirstOrDefault(p => p.StartsWith(partName) && p.Length >= partName.Length && p[partName.Length] == '=');
						if (part == null)
							throw new InvalidSpriteFontException(string.Format("Part not found: \"{0}\"", partName));
						return int.Parse(part.Substring(partName.Length + 1));
					}
					
					switch (parts[0])
					{
					case "common":
					{
						imageWidth = GetPartValueI("scaleW");
						imageHeight = GetPartValueI("scaleH");
						LineHeight = GetPartValueI("lineHeight");
						
						if (GetPartValueI("pages") > 1)
							throw new InvalidSpriteFontException("Multiple pages are not supported.");
						
						break;
					}
					case "page":
					{
						if (imageFileName != null)
							throw new InvalidSpriteFontException("Multiple pages are not supported.");
						
						string filePart = parts.FirstOrDefault(p => p.StartsWith("file="));
						if (filePart == null)
							throw new InvalidSpriteFontException("Page doesn't contain a file attribute.");
						
						imageFileName = filePart.Substring(5);
						if (imageFileName.Length != 0 && imageFileName[0] == '"' &&
						    imageFileName[imageFileName.Length - 1] == '"')
						{
							imageFileName = imageFileName.Substring(1, imageFileName.Length - 2);
						}
						
						break;
					}
					case "char":
					{
						m_characters.Add((char)GetPartValueI("id"), new Character
						{
							TextureX = GetPartValueI("x"),
							TextureY = GetPartValueI("y"),
							Width = GetPartValueI("width"),
							Height = GetPartValueI("height"),
							XOffset = GetPartValueI("xoffset"),
							YOffset = GetPartValueI("yoffset"),
							XAdvance = GetPartValueI("xadvance")
						});
						break;
					}
					case "kerning":
					{
						KerningPair pair = new KerningPair
						{
							First = (char)GetPartValueI("first"),
							Second = (char)GetPartValueI("second"),
							Amount = GetPartValueI("amount")
						};
						
						int pos = m_kerningPairs.BinarySearch(pair);
						m_kerningPairs.Insert(pos < 0 ? ~pos : pos, pair);
						
						break;
					}
					}
				}
				
				Texture = Texture2D.LoadAbsPath(Path.GetDirectoryName(path) + "/" + imageFileName);
				Texture.SetSwizzle(Texture2D.Swizzle.One, Texture2D.Swizzle.One, Texture2D.Swizzle.One, Texture2D.Swizzle.Red);
			}
		}
		
		public bool GetCharacter(char c, out Character character)
		{
			return m_characters.TryGetValue(c, out character);
		}
		
		public int GetKerning(char first, char second)
		{
			int pos = m_kerningPairs.BinarySearch(new KerningPair { First = first, Second = second});
			return pos < 0 ? 0 : m_kerningPairs[pos].Amount;
		}
		
		public bool SupportsCharacter(char c)
		{
			return m_characters.ContainsKey(c);
		}
		
		public Vector2 MeasureString(string text)
		{
			float currentLineWidth = 0;
			
			float width = 0;
			float height = 0;
			
			foreach (char c in text)
			{
				if (c == '\n')
				{
					width = Math.Max(width, currentLineWidth);
					currentLineWidth = 0;
					height += LineHeight;
					continue;
				}
				
				if (!m_characters.TryGetValue(c, out Character character))
					continue;
				
				currentLineWidth += character.XAdvance; //TODO: Add kerning
			}
			
			if (currentLineWidth > 0)
			{
				width = Math.Max(width, currentLineWidth);
				height += LineHeight;
			}
			
			return new Vector2(width, height);
		}
		
		public void Dispose()
		{
			Texture.Dispose();
		}
	}
}
