﻿using System;

namespace ReClassNET.Nodes
{
	class UTF8TextPtrNode : BaseTextPtrNode
	{
		public override int Draw(ViewInfo view, int x, int y)
		{
			var ptr = view.Memory.ReadObject<IntPtr>(Offset);
			var str = view.Memory.Process.ReadUTF8String(ptr, 64);

			return DrawText(view, x, y, "Text8Ptr ", MemorySize, str);
		}
	}
}