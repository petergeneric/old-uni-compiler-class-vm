using System;
using System.Collections;
using System.Text;

namespace TargetVM
{
    struct CpuInstruction
    {
        public Core vm;
        public byte opcode;
        public byte register;
        public byte indirections;
        public ushort operand;

        public bool isSmart;
        public bool hasReg;
        public bool hasOperand;
        public bool hasIndirection;

        public void setOperandsSmart(int operand, int indirections, int reg)
        {
            isSmart = true;
            if (operand != int.MaxValue)
            {
                this.operand = (ushort) operand;
                this.hasOperand = true;
            }
            else
            {
                this.operand = 0;
                this.hasOperand = false;
            }

            if (indirections != int.MaxValue)
            {
                this.indirections = (byte)indirections;
                this.hasIndirection = true;
            }
            else
            {
                this.indirections = 0;
                this.hasIndirection = false;
            }

            if (reg != int.MaxValue)
            {
                register = (byte)reg;
                hasReg = true;
            }
            else
            {
                register = 0;
                hasReg = false;
            }
        }


        public new String ToString()
        {
            OpCode op = (OpCode)opcode;

            if (isSmart)
            {
                return op + "\t" + (hasOperand ? "" + operand : "") + (hasOperand && hasReg ? ",[" : "") + (hasReg ? getRegisterName(register) : "") + (hasIndirection ? ", " + indirections : "") + (hasOperand && hasReg ? "]" : "");
            }
            else
            {
                return op + "\t" + operand + ",[" + getRegisterName(register) + ", " + indirections + "]";
            }
        }

        private string getRegisterName(byte reg)
        {
            switch (reg)
            {
                case 0: return "BP";
                case 1: return "FP";
                case 2: return "MP";
                case 3: return "SP";
                case 255: return "";
                default:
                    throw new ArgumentOutOfRangeException("Invalid register number: " + reg);
            }
        }

        /// <summary>Generic helper function</summary>
        /// <param name="number"></param>
        /// <returns></returns>
        public String toBinary(ushort number)
        {
            String s = String.Format("{0:X}", number);
            String buffer = "";
            foreach (char c in s) {
                switch (c)
                {
                    case '0': buffer += "0000"; break;
                    case '1': buffer += "0001"; break;
                    case '2': buffer += "0010"; break;
                    case '3': buffer += "0011"; break;
                    case '4': buffer += "0100"; break;
                    case '5': buffer += "0101"; break;
                    case '6': buffer += "0110"; break;
                    case '7': buffer += "0111"; break;
                    case '8': buffer += "1000"; break;
                    case '9': buffer += "1001"; break;
                    case 'A': buffer += "1010"; break;
                    case 'B': buffer += "1011"; break;
                    case 'C': buffer += "1100"; break;
                    case 'D': buffer += "1101"; break;
                    case 'E': buffer += "1110"; break;
                    case 'F': buffer += "1111"; break;
                    default:  buffer += "?";    break;
                }
            }

            return buffer;
        }

        public ushort this[int index]
        {
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
        }

        /// <summary>Offsets the operand by the value in L indirections from register r.</summary>
        /// <returns></returns>
        /// <todo>Handle overflows?</todo>
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
                val = vm.memGetWord(val);
            }

            return val;
        }
    }


    /// <summary>The core of the virtual machine - memory, registers and rudimentary stack functions</summary>
    class Core
    {
        public ushort[] memory = new ushort[ushort.MaxValue];

        public ushort PC = 0;   // Program Counter
        public ushort SP = 0;  // Stack Pointer
        public ushort BP = 0;  // Base Pointer
        public ushort FP = 2000;  // Frame Pointer
        public ushort MP = 0;  // Mark Pointer
        public ushort PSR = 0; // Program Status Register (should this be special?)

        // Misc debugging statistics:
        private uint jumps = 0; // Stores the number of jump ops the processor has executed
        private uint noops = 0; // Stores the number of noop ops the processor has executed


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

        public void setPSR_H(bool value)
        {
            if (value) // If we're setting the flag:
            {
                this.PSR = (ushort)(this.PSR | 0x00);
            }
            else // If we're unsetting the flag:
            {
                this.PSR = (ushort)(this.PSR & 0xFF);
            }

            throw new NotImplementedException("Setting PSR bits is not yet supported");
        }


        /// <summary>Pops a word off the stack</summary>
        /// <returns>The word on top of the stack</returns>
        public ushort pop()
        {
            SP--;
            return memGetWord(SP);
        }

        /// <summary>Peeks at the word on the top of the stack</summary>
        /// <returns>The word on top of the stack</returns>
        public ushort peek()
        {
            return memGetWord((ushort)(SP - 1));
        }

        /// <summary>Pushes a value onto the stack</summary>
        /// <param name="val">Pushes a word onto the top of the stack</param>
        public void push(ushort val)
        {
            memSetWord(SP, val);
            SP = (ushort)(SP + 1);
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

        /// <summary>Retrieves a word from memory</summary>
        /// <param name="addr">The address to retrieve</param>
        /// <returns>The data as a ushort</returns>
        public ushort memGetWord(ushort addr)
        {
            return memory[addr];
        }

        /// <summary>Retrieves a word from memory</summary>
        /// <param name="addr">The address to retrieve</param>
        /// <returns>The data as a ushort</returns>
        public ushort memGetWord(int addr)
        {
            return memory[addr];
        }

        /// <summary>Sets a word in memory</summary>
        /// <param name="addr">The address</param>
        /// <param name="val">The value</param>
        public void memSetWord(ushort addr, ushort val)
        {
            memory[addr] = val;
        }

        /// <summary>Sets a word in memory</summary>
        /// <param name="addr">The address</param>
        /// <param name="val">The value</param>
        public void memSetWord(int addr, ushort val)
        {
            memory[addr] = val;
        }


        public CpuInstruction getNextInstruction()
        {
            CpuInstruction op = new CpuInstruction();

            op.vm = this;
            op[0] = memGetWord(PC);
            op[1] = memGetWord((ushort)(PC + 1));

            // Advance 2 words
            PC += 2;

            return op;
        }





        /// <summary>Branches execution to the specified memory address</summary>
        /// <param name="address">The memory address to branch to</param>
        public void branch(ushort address)
        {
            ++jumps;
            PC = address; // Branch execution to the specified address
        }

        /// <summary>Executes the no-op instruction</summary>
        public void noop()
        {
            Console.WriteLine("noop!");
            Console.ReadLine();
            ++noops;
        }


        /// <summary>Returns the contents of the core (excluding memory)</summary>
        public Hashtable coreDump()
        {
            Hashtable d = new Hashtable();
            //d.Add("mem.full", memory);
            d.Add("reg.PC", PC);
            d.Add("reg.SP", SP);
            d.Add("reg.BP", BP);
            d.Add("reg.MP", MP);
            d.Add("reg.FP", FP);
            d.Add("reg.PSR", PSR);
            d.Add("jumps", jumps);
            d.Add("noops", noops);

            return d;
        }





        public void add()
        {
            short a = (short)this.pop();
            short b = (short)this.pop();

            int result = a + b;

            if (a <= short.MaxValue || a >= short.MinValue)
            {
                this.push((ushort)((short)result));
            }
            else
            { // Over/under flow
                Console.WriteLine("Overflow while executing. Addr=" + (PC - 2));
            }
        }

        public void sub()
        {
            short a = (short)this.pop();
            short b = (short)this.pop();

            int result = a - b;

            if (a <= short.MaxValue || a >= short.MinValue)
            {
                this.push((ushort)((short)result));
            }
            else
            { // Over/under flow
                Console.WriteLine("Overflow while executing. Addr=" + (PC - 2));
            }
        }

        public void mul()
        {
            short a = (short)this.pop();
            short b = (short)this.pop();

            int result = a * b;

            if (a <= short.MaxValue || a >= short.MinValue)
            {
                this.push((ushort)((short)result));
            }
            else
            { // Over/under flow
                Console.WriteLine("Overflow while executing. Addr=" + (PC - 2));
            }
        }

        public void div()
        {
            short a = (short)this.pop();
            short b = (short)this.pop();

            int result = a / b;

            if (a <= short.MaxValue || a >= short.MinValue)
            {
                this.push((ushort)((short)result));
            }
            else
            { // Over/under flow
                Console.WriteLine("Overflow while executing. Addr=" + (PC - 2));
            }
        }

        public void mod()
        {
            short a = (short)this.pop();
            short b = (short)this.pop();

            int result = a % b;

            if (a <= short.MaxValue || a >= short.MinValue)
            {
                this.push((ushort)((short)result));
            }
            else
            { // Over/under flow
                Console.WriteLine("Overflow while executing. Addr=" + (PC - 2));
            }
        }

        public void neg()
        {
            short a = (short)this.pop();

            this.push((ushort)-a);
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
