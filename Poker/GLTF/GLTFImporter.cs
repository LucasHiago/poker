using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using Newtonsoft.Json.Linq;

namespace Poker.GLTF
{
	public static class GLTFImporter
	{
		private enum ElementType
		{
			SCALAR,
			VEC2,
			VEC3,
			VEC4
		}
		
		private enum ComponentType
		{
			UInt8 = 5121,
			UInt16 = 5123,
			UInt32 = 5125,
			Float = 5126
		}
		
		private struct BufferView
		{
			public int BufferIndex;
			public int ByteOffset;
			public int ByteStride;
		}
		
		private struct Accessor
		{
			public int BufferIndex;
			public int ByteOffset;
			public int ByteStride;
			public ComponentType ComponentType;
			public int ElementCount;
			public ElementType ElementType;
		}
		
		public static Model Import(string path)
		{
			JObject json = JObject.Parse(File.ReadAllText(path));
			
			//Parses buffers
			JArray buffersArray = (JArray)json["buffers"];
			byte[][] buffers = new byte[buffersArray.Count][];
			for (int i = 0; i < buffersArray.Count; i++)
			{
				buffers[i] = File.ReadAllBytes(Path.GetDirectoryName(path) + "/" + buffersArray[i]["uri"]);
			}
			
			//Parses buffer views
			JArray bufferViewsArray = (JArray)json["bufferViews"];
			BufferView[] bufferViews = new BufferView[bufferViewsArray.Count];
			for (int i = 0; i < bufferViewsArray.Count; i++)
			{
				bufferViews[i].BufferIndex = (int)bufferViewsArray[i]["buffer"];
				bufferViews[i].ByteOffset = (int)bufferViewsArray[i]["byteOffset"];
				bufferViews[i].ByteStride = (bufferViewsArray[i]["byteStride"]?.Value<int>()).GetValueOrDefault();
			}
			
			//Parses accessors
			JArray accessorsArray = (JArray)json["accessors"];
			Accessor[] accessors = new Accessor[accessorsArray.Count];
			for (int i = 0; i < accessorsArray.Count; i++)
			{
				BufferView view = bufferViews[(int)accessorsArray[i]["bufferView"]];
				accessors[i].BufferIndex = view.BufferIndex;
				accessors[i].ElementCount = (int)accessorsArray[i]["count"];
				accessors[i].ByteOffset = view.ByteOffset + (accessorsArray[i]["byteOffset"]?.Value<int>()).GetValueOrDefault();
				accessors[i].ComponentType = (ComponentType)accessorsArray[i]["componentType"].Value<int>();
				accessors[i].ElementType = (ElementType)Enum.Parse(typeof(ElementType), accessorsArray[i]["type"].ToString());
				
				int componentsPerElement = 0;
				switch (accessors[i].ElementType)
				{
					case ElementType.SCALAR:
						componentsPerElement = 1;
						break;
					case ElementType.VEC2:
						componentsPerElement = 2;
						break;
					case ElementType.VEC3:
						componentsPerElement = 3;
						break;
					case ElementType.VEC4:
						componentsPerElement = 4;
						break;
				}
				
				if (view.ByteStride != 0)
				{
					accessors[i].ByteStride = view.ByteStride;
				}
				else
				{
					switch (accessors[i].ComponentType)
					{
					case ComponentType.UInt8:
						accessors[i].ByteStride = 1 * componentsPerElement;
						break;
					case ComponentType.UInt16:
						accessors[i].ByteStride = 2 * componentsPerElement;
						break;
					case ComponentType.UInt32:
					case ComponentType.Float:
						accessors[i].ByteStride = 4 * componentsPerElement;
						break;
					}
				}
			}
			
			JArray meshesArray = (JArray)json["meshes"];
			List<Mesh> meshes = new List<Mesh>();
			foreach (JObject meshEl in meshesArray)
			{
				string baseName = meshEl["name"].ToString();
				
				JArray primitivesArray = (JArray)meshEl["primitives"];
				for (int p = 0; p < primitivesArray.Count; p++)
				{
					int indicesAccessor = (int)primitivesArray[p]["indices"];
					int numIndices = accessors[indicesAccessor].ElementCount;
					byte[] indicesBuffer = buffers[accessors[indicesAccessor].BufferIndex];
					int indicesOffset = accessors[indicesAccessor].ByteOffset;
					int indicesStride = accessors[indicesAccessor].ByteStride;
					
					uint[] indices = new uint[numIndices];
					switch (accessors[indicesAccessor].ComponentType)
					{
						case ComponentType.UInt8:
							for (int i = 0; i < numIndices; i++)
								indices[i] = indicesBuffer[indicesOffset + i * indicesStride];
							break;
						case ComponentType.UInt16:
							for (int i = 0; i < numIndices; i++)
								indices[i] = BitConverter.ToUInt16(indicesBuffer, indicesOffset + i * indicesStride);
							break;
						case ComponentType.UInt32:
							for (int i = 0; i < numIndices; i++)
								indices[i] = BitConverter.ToUInt32(indicesBuffer, indicesOffset + i * indicesStride);
							break;
					}
					
					JObject attributesEl = (JObject)primitivesArray[p]["attributes"];
					JValue positionEl = attributesEl["POSITION"] as JValue;
					JValue normalEl = attributesEl["NORMAL"] as JValue;
					JValue texCoordEl = attributesEl["TEXCOORD_0"] as JValue;
					
					if (normalEl == null || accessors[(int)normalEl].ElementType != ElementType.VEC3 ||
					    accessors[(int)normalEl].ComponentType != ComponentType.Float)
					{
						throw new InvalidGLTFException("Invalid normal accessor.");
					}
					
					if (texCoordEl != null && (accessors[(int)texCoordEl].ElementType != ElementType.VEC2 ||
					                           accessors[(int)texCoordEl].ComponentType != ComponentType.Float))
					{
						throw new InvalidGLTFException("Invalid texcoord accessor.");
					}
					
					int positionAccessor = (int)positionEl;
					int normalAccessor = (int)normalEl;
					
					int numVertices = accessors[positionAccessor].ElementCount;
					
					byte[] positionBuffer = buffers[accessors[positionAccessor].BufferIndex];
					byte[] normalBuffer = buffers[accessors[normalAccessor].BufferIndex];
					byte[] texCoordBuffer = texCoordEl == null ? null : buffers[accessors[(int)texCoordEl].BufferIndex];
					
					Vertex[] vertices = new Vertex[numVertices];
					for (int v = 0; v < numVertices; v++)
					{
						int posOffset = accessors[positionAccessor].ByteOffset + v * accessors[positionAccessor].ByteStride;
						vertices[v].Position.X = BitConverter.ToSingle(positionBuffer, posOffset + sizeof(float) * 0);
						vertices[v].Position.Y = BitConverter.ToSingle(positionBuffer, posOffset + sizeof(float) * 1);
						vertices[v].Position.Z = BitConverter.ToSingle(positionBuffer, posOffset + sizeof(float) * 2);
						
						int normalOffset = accessors[normalAccessor].ByteOffset + v * accessors[normalAccessor].ByteStride;
						vertices[v].Normal.X = BitConverter.ToSingle(normalBuffer, normalOffset + sizeof(float) * 0);
						vertices[v].Normal.Y = BitConverter.ToSingle(normalBuffer, normalOffset + sizeof(float) * 1);
						vertices[v].Normal.Z = BitConverter.ToSingle(normalBuffer, normalOffset + sizeof(float) * 2);
						vertices[v].Normal = Vector3.Normalize(vertices[v].Normal);
						
						if (texCoordBuffer != null)
						{
							int texCoordOffset = accessors[(int)texCoordEl].ByteOffset + v * accessors[(int)texCoordEl].ByteStride;
							vertices[v].TexCoord.X = BitConverter.ToSingle(texCoordBuffer, texCoordOffset);
							vertices[v].TexCoord.Y = BitConverter.ToSingle(texCoordBuffer, texCoordOffset + sizeof(float));
						}
					}
					
					Vector3[] tangents1 = new Vector3[numVertices];
					Vector3[] tangents2 = new Vector3[numVertices];
					for (int i = 0; i < numIndices; i += 3)
					{
						Vector3 dp0 =  vertices[indices[i + 1]].Position - vertices[indices[i]].Position;
						Vector3 dp1 =  vertices[indices[i + 2]].Position - vertices[indices[i]].Position;
						Vector2 dtc0 = vertices[indices[i + 1]].TexCoord - vertices[indices[i]].TexCoord;
						Vector2 dtc1 = vertices[indices[i + 2]].TexCoord - vertices[indices[i]].TexCoord;
						
						float div = dtc0.X * dtc1.Y - dtc1.X * dtc0.Y;
						if (Math.Abs(div) < 1E-6f)
							continue;
						
						float r = 1.0f / div;
						
						Vector3 d1 = new Vector3((dtc0.Y * dp0.X - dtc0.Y * dp1.X) * r, (dtc1.Y * dp0.Y - dtc0.Y * dp1.Y) * r, (dtc1.Y * dp0.Z - dtc0.Y * dp1.Z) * r);
						Vector3 d2 = new Vector3((dtc0.X * dp1.X - dtc1.X * dp0.X) * r, (dtc0.X * dp1.Y - dtc1.X * dp0.Y) * r, (dtc0.X * dp1.Z - dtc1.X * dp0.Z) * r);
						
						for (int j = 0; j < 3; j++)
						{
							uint index = indices[i + j];
							tangents1[index] += d1;
							tangents2[index] += d2;
						}
					}
					
					for (int v = 0; v < numVertices; v++)
					{
						if (tangents1[v].LengthSquared() < 1E-6f)
							continue;
						
						Vector3 normal = vertices[v].Normal;
						vertices[v].Tangent = Vector3.Normalize(tangents1[v] - normal * Vector3.Dot(normal, tangents1[v]));
						
						if (Vector3.Dot(Vector3.Cross(normal, vertices[v].Tangent), tangents2[v]) < 0.0f)
							vertices[v].Tangent = -vertices[v].Tangent;
					}
					
					string name = primitivesArray.Count == 1 ? baseName : $"{baseName}_{p}";
					meshes.Add(new Mesh(name, vertices, indices));
				}
			}
			
			return new Model(meshes.ToArray());
		}
	}
}
