using System;
using System.Runtime.InteropServices;

namespace Poker
{
	public unsafe class UniformBuffer : IDisposable
	{
		[DllImport("Native")]
		private static extern IntPtr UB_Create(ulong size);
		[DllImport("Native")]
		private static extern void UB_Destroy(IntPtr handle);
		[DllImport("Native")]
		private static extern void* UB_GetMapping(IntPtr handle);
		[DllImport("Native")]
		private static extern void UB_Flush(IntPtr handle);
		[DllImport("Native")]
		private static extern void UB_Bind(IntPtr handle, uint unit);
		
		private readonly IntPtr m_handle;
		
		public UniformBuffer(ulong size)
		{
			m_handle = UB_Create(size);
		}
		
		~UniformBuffer()
		{
			UB_Destroy(m_handle);
		}
		
		public void Dispose()
		{
			UB_Destroy(m_handle);
			GC.SuppressFinalize(this);
		}
		
		public void* GetMapping()
		{
			return UB_GetMapping(m_handle);
		}
		
		public void Flush()
		{
			UB_Flush(m_handle);
		}
		
		public void Bind(uint unit)
		{
			UB_Bind(m_handle, unit);
		}
	}
}
