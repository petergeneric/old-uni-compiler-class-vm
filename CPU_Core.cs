using System;
using System.Collections;

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
    /// <summary>The core of the virtual machine - memory, registers and rudimentary stack functions</summary>
    class Core
    {
        public ushort[] memory = new ushort[ushort.MaxValue];

        public ushort PC = 0;   // Program Counter
        public ushort SP = 0;  // Stack Pointer
        public ushort BP = 0;  // Base Pointer
        public ushort FP = 0;  // Frame Pointer
        public ushort MP = 0;  // Mark Pointer
        public ushort PSR = 0; // Program Status Register (psr[CVEHMRI] properties exist)
        public bool halted = false; // Duplicate of PSR[H] state - provided for performance

        #region Easy register access
        public ushort getRegister(byte reg)
        {
            switch (reg)
            {
                case 0: return BP;
                case 1: return FP;
                case 2: return MP;
                case 3: return SP;
                default:
                    throw new ArgumentOutOfRangeException("Invalid register number " + reg + " at addr=" + (PC - 2));
            }
        }

        public void setRegister(byte reg, ushort value)
        {
            switch (reg)
            {
                case 0: BP = value; return;
                case 1: FP = value; return;
                case 2: MP = value; return;
                case 3: SP = value; return;
                default:
                    throw new ArgumentOutOfRangeException("Invalid register number " + reg + " at addr=" + (PC - 2));
            }
        }

        /// <summary>Increments the specified register by the exact value specified.</summary>
        /// <param name="reg">The register's code</param>
        /// <param name="amount">The amount to add to the register</param>
        public void incRegister(byte reg, ushort amount)
        {
            switch (reg)
            {
                case 0: BP += amount; return;
                case 1: FP += amount; return;
                case 2: MP += amount; return;
                case 3: SP += amount; return;
                default:
                    throw new ArgumentOutOfRangeException("Invalid register number " + reg + " at addr=" + (PC - 2));
            }
        }
        #endregion

        #region PSR Bits
        public bool psrC
        {
            get
            {
                return getPsr(1);
            }
            set
            {
                setPsr(1, value);
            }
        }

        public bool psrV
        {
            get
            {
                return getPsr(2);
            }
            set
            {
                setPsr(2, value);
            }
        }

        public bool psrE
        {
            get
            {
                return getPsr(4);
            }
            set
            {
                setPsr(4, value);
            }
        }

        public bool psrH
        {
            get
            {
                return getPsr(8);
            }
            set
            {
                halted = value;
                setPsr(8, value);
            }
        }

        public bool psrM
        {
            get
            {
                return getPsr(16);
            }
            set
            {
                setPsr(16, value);
            }
        }

        public bool psrR
        {
            get
            {
                return getPsr(32);
            }
            set
            {
                setPsr(32, value);
            }
        }

        public bool psrI
        {
            get
            {
                return getPsr(64);
            }
            set
            {
                setPsr(64, value);
            }
        }


        #region PSR helper functions
        private void setPsr(byte mask, bool value)
        {
            if (value)
            {
                this.PSR = (ushort) (this.PSR | mask);
            }
            else
            {
                this.PSR = (ushort) (this.PSR & ~mask); // AND it with the inversion of the mask
            }
        }

        private bool getPsr(byte mask)
        {
            return (this.PSR & mask) != 0;
        }
        #endregion
        #endregion


        /// <summary>Pops a word off the stack</summary>
        /// <returns>The word on top of the stack</returns>
        public ushort pop()
        {
            return memory[--SP];
        }

        /// <summary>Peeks at the word on the top of the stack</summary>
        /// <returns>The word on top of the stack</returns>
        public ushort peek()
        {
            return memory[SP - 1];
        }

        /// <summary>Pushes a value onto the stack</summary>
        /// <param name="val">Pushes a word onto the top of the stack</param>
        public void push(ushort val)
        {
            memory[SP++] = val;
        }

        /// <summary>Copy a certain number of words from one location in memory to another</summary>
        /// <param name="source">source</param>
        /// <param name="dest">destination</param>
        /// <param name="size">the number of words to copy</param>
        public void memCopyWord(ushort source, ushort dest, ushort size)
        {
            for (; size != 0; --size)
            {
                memory[dest++] = memory[source++];
            }
        }


        public CpuInstruction getNextInstruction()
        {
            CpuInstruction op = new CpuInstruction();

            op.vm = this;
            op[0] = memory[PC++];
            op[1] = memory[PC++];

            // The above also advances 2 words

            return op;
        }





        /// <summary>Branches execution to the specified memory address</summary>
        /// <param name="address">The memory address to branch to</param>
        public void branch(ushort address)
        {
            PC = address; // Branch execution to the specified address
        }


        /// <summary>Returns the contents of the registers</summary>
        public Hashtable coreDump()
        {
            Hashtable d = new Hashtable();
            d.Add("reg.PC", PC);
            d.Add("reg.SP", SP);
            d.Add("reg.BP", BP);
            d.Add("reg.MP", MP);
            d.Add("reg.FP", FP);
            d.Add("reg.PSR", PSR);

            return d;
        }



        public void incr(short amount)
        {
            short a = (short)this.pop();

            int result = a + amount;

            if (result <= short.MaxValue || result >= short.MinValue)
            {
                this.push((ushort)((short)result));
                this.psrV = false;
            }
            else
            { // Over/under flow
                this.psrV = true;
                this.psrH = this.psrC;

                // Restore the stack:
                this.push((ushort)a);
            }
        }

        public void add()
        {
            short a = (short)this.pop();
            short b = (short)this.pop();

            int result = a + b;

            if (result <= short.MaxValue || result >= short.MinValue)
            {
                this.push((ushort)((short)result));
                this.psrV = false;
            }
            else
            { // Over/under flow
                this.psrV = true;
                this.psrH = this.psrC;

                // Restore the stack
                this.push((ushort)a);
                this.push((ushort)b);
            }
        }

        public void sub()
        {
            short a = (short)this.pop();
            short b = (short)this.pop();

            int result = a - b;

            if (result <= short.MaxValue || result >= short.MinValue)
            {
                this.push((ushort)((short)result));
                this.psrV = false;
            }
            else
            { // Over/under flow
                this.psrV = true;
                this.psrH = this.psrC;

                // Restore the stack
                this.push((ushort)a);
                this.push((ushort)b);
            }
        }

        public void mul()
        {
            short a = (short)this.pop();
            short b = (short)this.pop();

            int result = a * b;

            if (result <= short.MaxValue || result >= short.MinValue)
            {
                this.push((ushort)((short)result));
                this.psrV = false;
            }
            else
            { // Over/under flow
                this.psrV = true;
                this.psrH = this.psrC;

                // Restore the stack
                this.push((ushort)a);
                this.push((ushort)b);
            }
        }

        public void div()
        {
            short a = (short)this.pop();
            short b = (short)this.pop();

            int result = a / b;

            if (result <= short.MaxValue || result >= short.MinValue)
            {
                this.push((ushort)((short)result));
                this.psrV = false;
            }
            else
            { // Over/under flow
                this.psrV = true;
                this.psrH = this.psrC;

                // Restore the stack
                this.push((ushort)a);
                this.push((ushort)b);
            }
        }

        public void mod()
        {
            short a = (short)this.pop();
            short b = (short)this.pop();

            int result = a % b;

            if (result <= short.MaxValue || result >= short.MinValue)
            {
                this.push((ushort)((short)result));
                this.psrV = false;
            }
            else
            { // Over/under flow
                this.psrV = true;
                this.psrH = this.psrC;

                // Restore the stack
                this.push((ushort)a);
                this.push((ushort)b);
            }
        }

        public void neg()
        {
            short a = (short)this.pop();

            int result = -a;

            if (result <= short.MaxValue || result >= short.MinValue)
            {
                this.push((ushort)((short)result));
                this.psrV = false;
            }
            else
            { // Over/under flow
                this.psrV = true;
                this.psrH = this.psrC;

                // Restore the stack
                this.push((ushort)a);
            }
        }

        public void land()
        {
            ushort a = this.pop();
            ushort b = this.pop();

            ushort result = (ushort)(a | b);

            this.push((ushort)result);
        }

        public void lor()
        {
            ushort a = this.pop();
            ushort b = this.pop();

            ushort result = (ushort)(a | b);

            this.push((ushort)result);
        }

        public void lxor()
        {
            ushort a = this.pop();
            ushort b = this.pop();

            ushort result = (ushort)(a ^ b);

            this.push((ushort)result);
        }


        public void lnot()
        {
            this.push((ushort)~this.pop());
        }

        public void lsl(ushort shiftBy)
        {
            ushort val = this.pop();
            val = (ushort)(val << (shiftBy % 16));

            push(val);
        }

        public void lsr(ushort shiftBy)
        {
            ushort val = this.pop();
            val = (ushort)(val >> (shiftBy % 16));

            push(val);
        }
    }
}
