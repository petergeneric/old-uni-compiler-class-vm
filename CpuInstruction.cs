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
    struct CpuInstruction
    {
        public Core vm;
        public byte opcode;
        public byte register;
        public byte indirections;
        public ushort operand;

        /// <summary>Decodes/encodes an instruction</summary>
        /// <param name="index">0 or 1</param>
        /// <returns></returns>
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
        /// <returns></returns>
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
                case OpCode.BLANK: return op.ToString(); // NONSTANDARD
                case OpCode.EXITS: return op.ToString(); // NONSTANDARD
#endif
                case OpCode.NOOP: return op.ToString();
                case OpCode.ADD: return op.ToString();
                case OpCode.SUB: return op.ToString();
                case OpCode.MUL: return op.ToString();
                case OpCode.DVD: return op.ToString();
                case OpCode.DREM: return op.ToString();
                case OpCode.LAND: return op.ToString();
                case OpCode.LOR: return op.ToString();
                case OpCode.INV: return op.ToString();
                case OpCode.NEG: return op.ToString();

                case OpCode.CLT: return op.ToString();
                case OpCode.CLE: return op.ToString();
                case OpCode.CEQ: return op.ToString();
                case OpCode.CNE: return op.ToString();

                case OpCode.EXIT: return op.ToString();
                case OpCode.HALT: return op.ToString();

                case OpCode.CHECK: return op.ToString();

                case OpCode.CHIN: return op.ToString();
                case OpCode.CHOUT: return op.ToString();

#if !NOEXTENDEDINSTRUCTIONS
                case OpCode.INTIN: return op.ToString();  // NONSTANDARD
                case OpCode.INTOUT: return op.ToString(); // NONSTANDARD
#endif

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
                    Console.WriteLine("Disassembly: Invalid Opcode!");
                    return "[INVALID MACHINE INSTRUCTION]";
            }
        }


        #region Disassembly helper functions
        private static string dasm(OpCode op, ushort operand)
        {
            return String.Format("{0:G6}\t{1}", op.ToString(), operand);
            //return op.ToString() + "\t" + operand;
        }

        // Specifically for INCREG; displays OP       REG, n
        private static string dasm(OpCode op, ushort operand, byte register)
        {
            return String.Format("{0:G6}\t{1}, {2}", op.ToString(), getRegisterName(register), operand);
        }

        private static string dasm(OpCode op, ushort operand, byte register, byte indirections)
        {
            return String.Format("{0:G6}\t{1},[{2}, {3}]", op.ToString(), operand, getRegisterName(register), indirections);
            //return op.ToString() + "\t" + operand + ",[" + getRegisterName(register) + ", " + indirections + "]";
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
