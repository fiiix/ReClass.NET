#include <vector>
#include <cstdint>
#include <beaengine/BeaEngine.h>

#include "NativeCore.hpp"

extern "C" bool DisassembleCode(RC_Pointer address, RC_Size length, RC_Pointer virtualAddress, InstructionData* instruction)
{
	DISASM disasm = {};
	disasm.Options = NasmSyntax + PrefixedNumeral;
#ifdef NATIVE_CORE_64
	disasm.Archi = 64;
#endif
	disasm.VirtualAddr = (UInt64)virtualAddress;
	disasm.EIP = (UIntPtr)address;
	disasm.SecurityBlock = (UInt32)length;

	auto disamLength = Disasm(&disasm);
	if (disamLength == OUT_OF_BLOCK || disamLength == UNKNOWN_OPCODE)
	{
		return false;
	}

	instruction->Length = disamLength;
	std::memcpy(instruction->Data, address, disamLength);
	MultiByteToUnicode(disasm.CompleteInstr, instruction->Instruction, 64);

	return true;
}
