using System;

// Copyright (c) 2006, Peter Wright <peter@peterphi.com>
// All rights reserved.
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
//
//     * Redistributions of source code must retain the above copyright
//       notice, this list of conditions and the following disclaimer.
//     * Redistributions in binary form must reproduce the above copyright
//       notice, this list of conditions and the following disclaimer in the
//       documentation and/or other materials provided with the distribution.
//     * The work or any derived work is made available for distribution
//       freely, and that the location is readily available to anyone who
//       wishes to download it.
//
// THIS SOFTWARE IS PROVIDED ``AS IS'' AND ANY EXPRESS OR IMPLIED WARRANTIES,
// INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND
// FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE
// AUTHOR BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY,
// OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
// SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
// INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
// CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
// ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF
// THE POSSIBILITY OF SUCH DAMAGE.

namespace TargetVM
{
    /// <summary>Structured view of an instruction</summary>
    class CpuInstruction
    {
        /// <summary>We need a reference to the machine's core so we can abstract indirections inside this class</summary>
        public Core vm;


        public byte opcode;
        public byte register;
        public byte indirections;
        public ushort operand;

        /// <summary>Decodes/encodes an instruction.</summary>
        /// <param name="index">0 or 1</param>
        /// <returns>the value at that address relative to the start of the instruction</returns>
        public ushort this[int index]
        {
            #region getter (encode)
            get
            {
                if (index == 0)
                {
                    ushort tmp = this.indirections;
                    tmp += (ushort)(this.register << 6);
                    tmp += (ushort)(this.opcode << 8);

                    return tmp;
                }
                else if (index == 1)
                {
                    return this.operand;
                }
                else
                {
                    throw new ArgumentOutOfRangeException("CpuInstruction only has 2 words");
                }
            }
            #endregion

            #region setter (decode)
            set
            {
                if (index == 0)
                {
                    this.opcode = (byte)((value & 0xFF00) >> 8);
                    this.register = (byte)((value & 0x00C0) >> 6);
                    this.indirections = (byte)(value & 0x003F);
                }
                else if (index == 1)
                {
                    this.operand = value;
                }
                else
                {
                    throw new ArgumentOutOfRangeException("CpuInstruction only has 2 words");
                }
            }
            #endregion
        }

        /// <summary>Offsets the operand by the value in L indirections from register r.</summary>
        /// <returns>the "m" (as defined in the spec) for this instruction</returns>
        public ushort getOffsetOperand()
        {
            return (ushort)(operand + indirect(vm.getRegister(register), indirections));
        }

        /// <summary>Indirects val numIndirections times</summary>
        /// <param name="val">The initial value / memory location</param>
        /// <param name="numIndirections">The number of indirections to perform</param>
        /// <returns>if numIndirections == 0, val is returned. Otherwise the value is calculated from memory.</returns>
        private ushort indirect(ushort val, byte numIndirections)
        {
            // This loop executes (indirections) times
            for (; numIndirections != 0; --numIndirections)
            {
                val = vm.memory[val];
            }

            return val;
        }

        #region Disassembly display
        /// <summary>Displays the disassembly of an instruction</summary>
        /// <returns>The disassembly as it would appear in an object code file</returns>
        public new string ToString()
        {
            OpCode op = (OpCode)opcode;


            switch (op)
            {
#if !NOEXTENDEDINSTRUCTIONS
                case OpCode.BLANK: return dasm(op); // NONSTANDARD
#endif
                case OpCode.NOOP: return dasm(op);
                case OpCode.ADD: return dasm(op);
                case OpCode.SUB: return dasm(op);
                case OpCode.MUL: return dasm(op);
                case OpCode.DVD: return dasm(op);
                case OpCode.DREM: return dasm(op);
                case OpCode.LAND: return dasm(op);
                case OpCode.LOR: return dasm(op);
                case OpCode.INV: return dasm(op);
                case OpCode.NEG: return dasm(op);

                case OpCode.CLT: return dasm(op);
                case OpCode.CLE: return dasm(op);
                case OpCode.CEQ: return dasm(op);
                case OpCode.CNE: return dasm(op);

                case OpCode.EXIT: return dasm(op);
                case OpCode.HALT: return dasm(op);

                case OpCode.CHECK: return dasm(op);

                case OpCode.CHIN: return dasm(op);
                case OpCode.CHOUT: return dasm(op);


                // Instructions with only an operand //

                case OpCode.LOADL: return dasm(op, operand);
                case OpCode.LOADI: return dasm(op, operand);
                case OpCode.STOREI: return dasm(op, operand);

                case OpCode.INCR: return dasm(op, operand);
                case OpCode.MOVE: return dasm(op, operand);
                case OpCode.SLL: return dasm(op, operand);
                case OpCode.SRL: return dasm(op, operand);
                case OpCode.BIDX: return dasm(op, operand);
                case OpCode.MARK: return dasm(op, operand);
                case OpCode.SETPSR: return dasm(op, operand);


                // Instructions with just a register //
                case OpCode.LOADR: return dasm(op, register);
                case OpCode.STORER: return dasm(op, register);

                // Instructions with an operand and a register //
                case OpCode.INCREG: return dasm(op, operand, register);

                // Instructions with all arguments //
                case OpCode.LOAD: return dasm(op, operand, register, indirections);
                case OpCode.LOADA: return dasm(op, operand, register, indirections);
                case OpCode.STORE: return dasm(op, operand, register, indirections);
                case OpCode.STZ: return dasm(op, operand, register, indirections);
                case OpCode.BRN: return dasm(op, operand, register, indirections);
                case OpCode.BZE: return dasm(op, operand, register, indirections);
                case OpCode.BNZ: return dasm(op, operand, register, indirections);
                case OpCode.BNG: return dasm(op, operand, register, indirections);
                case OpCode.BPZ: return dasm(op, operand, register, indirections);
                case OpCode.BVS: return dasm(op, operand, register, indirections);
                case OpCode.BES: return dasm(op, operand, register, indirections);
                case OpCode.CALL: return dasm(op, operand, register, indirections);
                case OpCode.SETSP: return dasm(op, operand, register, indirections);

                default:
                    return "[ILLEGAL OPCODE]";
            }
        }


        #region Disassembly helper functions
        private static string dasm(OpCode op) {
            return String.Format("{0:G6}\t\t", op.ToString());
        }
        private static string dasm(OpCode op, ushort operand)
        {
            return String.Format("{0:G6}\t{1}\t", op.ToString(), operand);
        }

        // specifically for STORER and LOADR
        private static string dasm(OpCode op, byte register) {
            return String.Format("{0:G6}\t{1}\t", op.ToString(), getRegisterName(register));
        }

        // Specifically for INCREG; displays OP       REG, n
        private static string dasm(OpCode op, ushort operand, byte register)
        {
            return String.Format("{0:G6}\t{1}, {2}\t", op.ToString(), getRegisterName(register), operand);
        }

        private static string dasm(OpCode op, ushort operand, byte register, byte indirections)
        {
            return String.Format("{0:G6}\t{1},[{2}, {3}]", op.ToString(), operand, getRegisterName(register), indirections);
        }

        /// <summary>Returns the human name of a register</summary>
        /// <param name="reg">The register id</param>
        /// <returns>The register name (or INVALID if not a valid register number)</returns>
        private static string getRegisterName(byte reg)
        {
            switch (reg)
            {
                case 0: return "BP";
                case 1: return "FP";
                case 2: return "MP";
                case 3: return "SP";
                default:
                    return "INVALID";
            }
        }
        #endregion
        #endregion
    }
}
