﻿using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using ReClassNET.Memory;
using ReClassNET.UI;
using ReClassNET.Util;

namespace ReClassNET.Nodes
{
	public class FunctionNode : BaseNode
	{
		private IntPtr address = IntPtr.Zero;
		private readonly List<string> instructions = new List<string>();

		public string Signature { get; set; } = "void function()";

		public ClassNode BelongsToClass { get; set; }

		private int memorySize = IntPtr.Size;
		/// <summary>Size of the node in bytes.</summary>
		public override int MemorySize => memorySize;

		public override string GetToolTipText(HotSpot spot, MemoryBuffer memory)
		{
			DisassembleRemoteCode(memory, spot.Address);

			return string.Join("\n", instructions);
		}

		public override int Draw(ViewInfo view, int x, int y)
		{
			Contract.Requires(view != null);

			if (IsHidden)
			{
				return DrawHidden(view, x, y);
			}

			AddSelection(view, x, y, view.Font.Height);
			AddDelete(view, x, y);
			AddTypeDrop(view, x, y);

			x += TextPadding;

			x = AddIcon(view, x, y, Icons.Function, -1, HotSpotType.None);

			var tx = x;

			x = AddAddressOffset(view, x, y);

			x = AddText(view, x, y, view.Settings.TypeColor, HotSpot.NoneId, "Function") + view.Font.Width;
			x = AddText(view, x, y, view.Settings.NameColor, HotSpot.NameId, Name) + view.Font.Width;

			x = AddOpenClose(view, x, y) + view.Font.Width;

			x = AddComment(view, x, y);

			var ptr = view.Address.Add(Offset);

			DisassembleRemoteCode(view.Memory, ptr);

			if (levelsOpen[view.Level])
			{
				y += view.Font.Height;
				x = AddText(view, tx, y, view.Settings.TypeColor, HotSpot.NoneId, "Signature:") + view.Font.Width;
				x = AddText(view, x, y, view.Settings.ValueColor, 0, Signature);

				y += view.Font.Height;
				x = AddText(view, tx, y, view.Settings.TextColor, HotSpot.NoneId, "Belongs to: ");
				x = AddText(view, x, y, view.Settings.ValueColor, HotSpot.NoneId, BelongsToClass == null ? "<None>" : $"<{BelongsToClass.Name}>");
				x = AddIcon(view, x, y, Icons.Change, 1, HotSpotType.ChangeType);

				y += view.Font.Height;
				foreach (var line in instructions)
				{
					y += view.Font.Height;

					AddText(view, tx, y, view.Settings.NameColor, HotSpot.ReadOnlyId, line);
				}
			}

			return y + view.Font.Height;
		}

		public override int CalculateHeight(ViewInfo view)
		{
			if (IsHidden)
			{
				return HiddenHeight;
			}

			var h = view.Font.Height;
			if (levelsOpen[view.Level])
			{
				h += instructions.Count() * view.Font.Height;
			}
			return h;
		}

		public override void Update(HotSpot spot)
		{
			base.Update(spot);

			if (spot.Id == 0) // Signature
			{
				Signature = spot.Text;
			}
		}

		private void DisassembleRemoteCode(MemoryBuffer memory, IntPtr address)
		{
			Contract.Requires(memory != null);

			if (this.address != address)
			{
				instructions.Clear();

				this.address = address;

				if (!address.IsNull() && memory.Process.IsValid)
				{
					memorySize = 0;

					var disassembler = new Disassembler(memory.Process.CoreFunctions);
					foreach (var instruction in disassembler.RemoteDisassembleFunction(memory.Process, address, 8192))
					{
						memorySize += instruction.Length;

						instructions.Add($"{instruction.Address.ToString(Constants.StringHexFormat)} {instruction.Instruction}");
					}

					ParentNode?.ChildHasChanged(this);
				}
			}
		}
	}
}
