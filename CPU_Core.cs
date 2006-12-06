#define FASTPSR


using System;
using System.Collections;

// DEFINEables:
// OPCACHE - Enables the operation cache. This will improve performance on code with LOTS of repetition (side-effect: some memory is wasted, instructions cannot be rewritten on the fly)
// FASTPSR - Optimises the PSR for setting. Retrieving the PSR value will incur a performance penalty. This makes sense so you should probably keep it defined



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
    /// <summary>The core of the virtual machine - memory, registers and rudimentary stack functions</summary>
    /// <remarks>Methods are marked as unsafe because we can guarantee safe memory access (memory.size=ushort.max;accessing memory with a ushort)</remarks>
    class Core
    {
        public ushort[] memory = new ushort[ushort.MaxValue];

#if OPCACHE
        private static readonly int OPCACHE_SIZE = 500;
        private CpuInstruction[] opCache = new CpuInstruction[OPCACHE_SIZE-1];
#endif
        
        public ushort PC = 0;   // Program Counter
        public ushort SP = 0;  // Stack Pointer
        public ushort BP = 0;  // Base Pointer
        public ushort FP = 0;  // Frame Pointer
        public ushort MP = 0;  // Mark Pointer
        
#if FASTPSR
        // Program Status Register - represents psr[CVEHMRI]
        public bool psrC = false, psrV=false, psrE=false, psrH=false, psrM=false, psrR=false, psrI=false;
#else
        public ushort PSR = 0; // Program Status Register - psr[CVEHMRI] properties exist
        public bool halted; // copy of psrH for performance
#endif

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

        #region PSR access (FAST and slow)
#if FASTPSR
        public unsafe ushort PSR {
            get {
                return (ushort) (
                    (psrC ?  1 : 0) +
                    (psrV ?  2 : 0) +
                    (psrE ?  4 : 0) +
                    (psrH ?  8 : 0) +
                    (psrM ? 16 : 0) +
                    (psrR ? 32 : 0) +
                    (psrI ? 64 : 0));
            }
            set {
                psrC = (value & 1) != 0;
                psrV = (value & 2) != 0;
                psrE = (value & 4) != 0;
                psrH = (value & 8) != 0;
                psrM = (value & 16) != 0;
                psrR = (value & 32) != 0;
                psrI = (value & 64) != 0;
            }
        }

        
#else
        #region Slow PSR Bits
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
#endif
        #endregion

        /// <summary>Pops a word off the stack</summary>
        /// <returns>The word on top of the stack</returns>
        public unsafe ushort pop()
        {
            return memory[--SP];
        }

        /// <summary>Pushes a value onto the stack</summary>
        /// <param name="val">Pushes a word onto the top of the stack</param>
        public unsafe void push(ushort val)
        {
            memory[SP++] = val;
        }

        /// <summary>Copy a certain number of words from one location in memory to another</summary>
        /// <param name="source">source</param>
        /// <param name="dest">destination</param>
        /// <param name="size">the number of words to copy</param>
        public unsafe void memCopyWord(ushort source, ushort dest, ushort size)
        {
            for (; size != 0; --size)
            {
                memory[dest++] = memory[source++];
            }
        }


#if !OPCACHE
        /// <summary>getNextInstruction() modifies this object to minimise memory & GC footprint</summary>
        private CpuInstruction obj = new CpuInstruction();
#endif

        public unsafe CpuInstruction getNextInstruction()
        {
#if OPCACHE
            // If this instruction is at a cacheable memory location...
            if (PC < OPCACHE_SIZE)
            {
                // Cache Miss - generate the object and insert it into the cache
                if (opCache[PC] == null)
                {
                    CpuInstruction obj = new CpuInstruction();
                    obj.vm = this;
                    obj[0] = memory[PC++];
                    obj[1] = memory[PC++];

                    opCache[PC-2] = obj;
                    
                    return obj;
                }
                else // Cache Hit - return the cached instruction
                {
                    CpuInstruction tmp = opCache[PC];
                    PC += 2;
                    return tmp;
                }
            }
            else
            {
                CpuInstruction obj = new CpuInstruction();
                obj.vm = this;

                obj[0] = memory[PC++];
                obj[1] = memory[PC++];

                return obj;
            }
#else
                // Uncached operation cache
                obj.vm = this;

                obj[0] = memory[PC++];
                obj[1] = memory[PC++];

                return obj;
#endif
        }





        #region core dump
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
        #endregion


        public unsafe void incr(short amount)
        {
            short a = (short)this.pop();

            int result = a + amount;

            if (result <= short.MaxValue && result >= short.MinValue)
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

        public unsafe void add()
        {
            short b = (short)this.pop();
            short a = (short)this.pop();

            int result = a + b;

            if (result <= short.MaxValue && result >= short.MinValue)
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

        public unsafe void sub()
        {
            short b = (short)this.pop();
            short a = (short)this.pop();

            int result = a - b;

            if (result <= short.MaxValue && result >= short.MinValue)
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

        public unsafe void mul()
        {
            short b = (short)this.pop();
            short a = (short)this.pop();

            int result = a * b;

            if (result <= short.MaxValue && result >= short.MinValue)
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

        public unsafe void div()
        {
            short b = (short)this.pop();
            short a = (short)this.pop();

            int result = a / b;

            if (result <= short.MaxValue && result >= short.MinValue)
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

        public unsafe void mod()
        {
            short b = (short)this.pop();
            short a = (short)this.pop();

            int result = a % b;

            if (result <= short.MaxValue && result >= short.MinValue)
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

        public unsafe void neg()
        {
            short a = (short)this.pop();

            int result = -a;

            if (result <= short.MaxValue && result >= short.MinValue)
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

        public unsafe void land()
        {
            ushort b = this.pop();
            ushort a = this.pop();

            ushort result = (ushort)(a | b);

            this.push((ushort)result);
        }

        public unsafe void lor()
        {
            ushort b = this.pop();
            ushort a = this.pop();

            ushort result = (ushort)(a | b);

            this.push((ushort)result);
        }

        public unsafe void lxor()
        {
            ushort b = this.pop();
            ushort a = this.pop();

            ushort result = (ushort)(a ^ b);

            this.push((ushort)result);
        }


        public unsafe void lnot()
        {
            this.push((ushort)~this.pop());
        }

        public unsafe void lsl(ushort shiftBy)
        {
            ushort val = this.pop();
            val = (ushort)(val << (shiftBy % 16));

            push(val);
        }

        public unsafe void lsr(ushort shiftBy)
        {
            ushort val = this.pop();
            val = (ushort)(val >> (shiftBy % 16));

            push(val);
        }
    }
}
