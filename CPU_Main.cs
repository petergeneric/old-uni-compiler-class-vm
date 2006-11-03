using System;
using System.Collections;
using System.Text;

namespace TargetVM
{

    class Decoder
    {
        public Core vm;
        public CpuInstruction op;

        public bool monitorBeep = true;
        public bool debug = true;
        public bool halted = false;
        public bool noopBreak = true;
        public bool noopBeep = false;
        public bool haltBreak = true;
        public int addrBreak = -1;
        public int monitorSkipInstructions = 0;

        public Decoder(Core vm)
        {
            this.vm = vm;
        }

        public void monitor()
        {
            if (monitorSkipInstructions != 0)
            {
                if (--monitorSkipInstructions > 0)
                {
                    return; // Do not display the monitor yet
                }
            }
            else if (addrBreak != -1) // If we have been instructed to break at a specific address:
            {
                if (vm.PC - 2 != addrBreak)
                {
                    return;
                }
                else
                {
                    addrBreak = -1;
                }
            }

            string cmd;
            string[] cmds;
            //if (monitorBeep) System.Media.SystemSounds.Beep.Play();

            Console.Write("\n");

            while (true)
            {

                Console.Write("[{0}] Monitor>", (vm.PC - 2));
                cmd = Console.ReadLine().Trim();
                cmds = cmd.Split(new char[] {' '});
                
                switch (cmds[0].ToLower())
                {
                    case null: case "": case "c": case "continue":
                        return;
                    case "next": case "n":
                        debug = true;
                        return;

                    case "r": case "run":
                        debug = false;
                        return;

                    case "k": case "skip": // Skip n executions:
                        monitorSkipInstructions = int.Parse(cmds[1]);
                        Console.WriteLine("Skipping monitor for next {0} instruction(s).", monitorSkipInstructions);
                        return;

                    case "b": case "break": // Break at a specific address
                        addrBreak = ushort.Parse(cmds[1]);
                        Console.WriteLine("Setting breakpoint at address {0}.", addrBreak);
                        return;

                    case "s": case "stop": case "exit": case "quit": case "q": // halt execution and terminate
                        Console.WriteLine("Target Monitor: goodbye.");
                        System.Environment.Exit(0);
                        return;

                    case "hb":
                    case "haltbreak": // examines / sets haltbreak
                        Console.WriteLine("haltBreak={0}", haltBreak);

                        if (cmds.Length == 2)
                        {
                            haltBreak = parseBool(cmds[1]);
                            Console.WriteLine("haltBreak={0}\tCHANGED", haltBreak);
                        }
                        break;

                    case "nb": case "noopbreak": // examines / sets noopBreak
                        Console.WriteLine("noopBreak={0}", noopBreak);

                        if (cmds.Length == 2)
                        {
                            noopBreak = parseBool(cmds[1]);
                            Console.WriteLine("noopBreak={0}\tCHANGED", noopBreak);
                        }
                        break;

                    case "j": case "jump":


                        if (cmds.Length == 2)
                        {
                            vm.PC = ushort.Parse(cmds[1]);
                            Console.WriteLine("Jumping to {0}", vm.PC);
                            this.op = vm.getNextInstruction(); // Decode the instruction and execute it instead
                        }
                        break;

                    case "d": case "decode":
                        for (int i = 1; i < cmds.Length; i++)
                        {
                            ushort addr = ushort.Parse(cmds[i]);
                            CpuInstruction memop = new CpuInstruction();
                            memop[0] = vm.memGetWord(addr);
                            memop[1] = vm.memGetWord(addr+1);
                            Console.WriteLine("{0}:\t{1}", addr, memop.ToString());
                        }

                        if (cmds.Length == 1) {
                            Console.WriteLine("{0}\t{1}", (vm.PC - 2), op.ToString());
                        }

                        break;
                    case "i": case "inspect":
                        for (int i = 1; i < cmds.Length; i++)
                        {
                            int addr = Math.Abs(int.Parse(cmds[i]));

                            if (cmds[i].StartsWith("-"))
                            {
                                Console.WriteLine("{0}:\t0x{1:X4} == {1}s", addr, (short) vm.memGetWord(addr));
                            }
                            else
                            {
                                Console.WriteLine("{0}:\t0x{1:X4} == {1}u", addr, vm.memGetWord(addr));
                            }
                            
                        }
                        break;
                    case "p": case "peek":
                        int pitems = 1;
                        bool peekSigned = false;
                        if (cmds.Length == 2)
                        {
                            pitems = Math.Abs(int.Parse(cmds[1]));
                            peekSigned = cmds[1].StartsWith("-");
                        }


                        for (int offset = 1; offset <= pitems; offset++)
                        {
                            ushort w = vm.memGetWord(vm.SP - offset);
                            if (peekSigned)
                            {
                                Console.WriteLine("{0}:\t0x{1:X4} == {1}s", vm.SP - offset, (short) w);
                            }
                            else
                            {
                                Console.WriteLine("{0}:\t0x{1:X4} == {1}u", vm.SP - offset, w);
                            }
                        }

                        break;

                    case "g": case "registers": case "register":
                        Hashtable core = vm.coreDump();
                        foreach (string key in core.Keys)
                        {
                            Console.WriteLine("{0}\t= {1}", key, core[key]);
                        }

                        break;

                    case "?": case "help":
                        Console.WriteLine("TARGET MONITOR");
                        Console.WriteLine("Commands that take -n consider it to be a signed access of n.");
                        Console.WriteLine("d {n}      - Displays a rough decode of the instruction[s] at n.");
                        Console.WriteLine("             Current instruction displayed if none are specified");
                        Console.WriteLine("             (decode)");
                        Console.WriteLine("p [-][n]   - Displays the top n items on the stack. (peek)");
                        Console.WriteLine("i [-]{n}   - Displays values stored in memory location[s] n.");
                        Console.WriteLine("j addr     - Branches immediately to ADDR. (jump)");
                        Console.WriteLine("q          - Terminates the VM immediately");
                        Console.WriteLine("c          - Resumes execution; monitor state unchanged");
                        Console.WriteLine("             (continue, <ENTER>)");
                        Console.WriteLine("r          - Resumes execution; monitor disabled");
                        Console.WriteLine("n          - Resumes execution; monitor enabled");
                        Console.WriteLine("k n        - Hides monitor for another n operations. (skip)");
                        Console.WriteLine("b n        - Hides monitor until operation at n. (break)");
                        Console.WriteLine("g [-]      - Displays all registers (registers)");
                        
                        break;

                    default:
                        Console.WriteLine("Monitor: Unknown command");
                        break;
                }
            }
        }

        public bool parseBool(string value)
        {
            switch (value.ToLower())
            {
                case "no": case "false": case "f": case "0": return false;
                case "yes": case "true": case "t": case "1": return true;
                default:
                    return false;
            }
        }


        /// <summary>Decodes an instruction</summary>
        public void tick()
        {
            if (halted)
            {
                return;
            }

            op = vm.getNextInstruction();

            if (debug)
            {
                monitor();
            }

            switch ((OpCode)op.opcode)
            {
                case OpCode.NOOP: // Let the CPU decide what to do with a noop
                    //if (noopBeep) System.Media.SystemSounds.Beep.Play();
                    
                    // Allow the monitor to reassert itself after the next noop
                    if (noopBreak)
                    {
                        debug = true;
                    }
                    break;

                // MATHS OPERATIONS //
                case OpCode.ADD:
                    vm.add(); break;
                case OpCode.SUB:
                    vm.sub(); break;
                case OpCode.DVD:
                    vm.div(); break;
                case OpCode.MUL:
                    vm.mul(); break;

                // LOGICAL OPERATIONS //
                case OpCode.LOR:
                    vm.lor(); break;
                case OpCode.INV:
                    vm.lnot(); break;
                case OpCode.NEG:
                    vm.neg(); break;
                case OpCode.LAND:
                    vm.land(); break;
                case OpCode.SLL:
                    vm.lsl(op.operand); break;
                case OpCode.SRL:
                    vm.lsr(op.operand); break;

                // COMPARISONS //
                case OpCode.CLT: // if (pop() < pop()) push(1) else push(0);
                    if ((short)vm.pop() < (short)vm.pop())
                    {
                        vm.push(1);
                    }
                    else
                    {
                        vm.push(0);
                    }
                    break;
                case OpCode.CLE: // if (pop() <= pop()) push(1) else push(0);
                    if ((short)vm.pop() <= (short)vm.pop())
                    {
                        vm.push(1);
                    }
                    else
                    {
                        vm.push(0);
                    }
                    break;
                case OpCode.CEQ: // if (pop() == pop()) push(1) else push(0);
                    if (vm.pop() == vm.pop())
                    {
                        vm.push(1);
                    }
                    else
                    {
                        vm.push(0);
                    }
                    break;
                case OpCode.CNE: // if (pop() != pop()) push(1) else push(0);
                    if (vm.pop() != vm.pop())
                    {
                        vm.push(1);
                    }
                    else
                    {
                        vm.push(0);
                    }
                    break;


                // BRANCHING //
                case OpCode.BRN:
                    vm.branch(op.getOffsetOperand()); break;
                case OpCode.BIDX:
                    vm.PC += (ushort)(op.operand * vm.pop());
                    break;
                case OpCode.BZE: // Branch to m if pop() == 0
                    if (vm.pop() == 0)
                    {
                        vm.branch(op.getOffsetOperand());
                    }
                    break;
                case OpCode.BNZ: // Branch to m if pop() != 0
                    if (vm.pop() != 0)
                    {
                        vm.branch(op.getOffsetOperand());
                    }
                    break;
                case OpCode.BNG: // Branch to m if pop() < 0
                    if ((short)vm.pop() < 0)
                    {
                        vm.branch(op.getOffsetOperand());
                    }
                    break;
                case OpCode.BPZ: // Branch to m if pop() >= 0
                    if ((short)vm.pop() >= 0)
                    {
                        vm.branch(op.getOffsetOperand());
                    }
                    break;
                case OpCode.BVS: // unknown
                    // if PSR(V) != 0, branch to m
                    break;
                case OpCode.BES: // unknown
                    // if PSR(E) != 0, branch to m
                    break;

                // SUBROUTINES //
                case OpCode.MARK: // Set MP to SP and increment SP by m
                    vm.MP = vm.SP;
                    vm.SP += op.operand;
                    break;
                case OpCode.CALL: // Store FP and PC on the MP stack; FP=MP; PC=m
                    vm.memSetWord((ushort)(vm.MP + 1), vm.FP);
                    vm.memSetWord((ushort) (vm.MP + 2), vm.PC);
                    vm.FP = vm.MP;
                    vm.branch(op.getOffsetOperand());
                    break;
                case OpCode.EXIT: // Restore FP and PC from the MP stack
                    vm.SP = vm.FP;
                    vm.FP = vm.memGetWord(vm.SP + 1);
                    vm.PC = vm.memGetWord(vm.SP + 2);
                    break;

                // LOADING //
                case OpCode.LOADL: // Load a value (operand)
                    vm.push(op.operand); break;
                case OpCode.LOADR: // Load a value addressed by a register
                    vm.push(vm.memGetWord(op.register)); break;
                case OpCode.LOAD: // Load the value of the offset operand
                    vm.push(vm.memGetWord(op.getOffsetOperand())); break;
                case OpCode.LOADA: // Load the address of the offset operand
                    vm.push(op.getOffsetOperand()); break;
                case OpCode.LOADI: // Load (operand) words onto the stack, source address on the top of the stack
                    {
                        // Could be more efficiently represented with memCopyWord. This way is more maintainable
                        ushort src = vm.pop(); // get the src address
                        ushort srcMax = (ushort)(src + (op.operand * sizeof(short)));

                        for (; src < srcMax; src += 2)
                        {
                            vm.push(vm.memGetWord(src));
                        }

                        break;
                    }

                // STORING //
                case OpCode.STORER: // Pop, using a register as a destination address
                    vm.memSetWord(op.register, vm.pop());
                    break;
                case OpCode.STORE: // Pop, using m as the destination address
                    vm.memSetWord(op.getOffsetOperand(), vm.pop());
                    break;
                case OpCode.STOREI: // Pop (operand) words from the stack
                    {
                        // Could be more efficiently represented with memCopyWord. This way is more maintainable
                        ushort dest = vm.pop();
                        for (int i = op.operand; i != 0; --i)
                        {
                            ushort val = vm.pop();
                            vm.memSetWord(dest, val);

                            dest += sizeof(ushort);
                        }
                        
                        break;
                    }

                case OpCode.INCR: // Increment the top of the stack by operand
                    vm.push(op.operand);
                    vm.add();
                    break;
                case OpCode.STZ: // Store 0 to memory location m
                    vm.memSetWord(op.getOffsetOperand(), 0);
                    break;
                case OpCode.INCREG: // Increment register by n words
                    vm.incRegister(op.register, (ushort)(sizeof(ushort) * op.operand));
                    break;

                case OpCode.MOVE: // Copy n words from (SP-2) to (SP-1)
                    ushort mdest = vm.pop();
                    ushort msrc = vm.pop();

                    vm.memCopyWord(msrc, mdest, op.operand);
                    break;

                case OpCode.SETSP:
                    vm.SP = op.getOffsetOperand();
                    break;
                case OpCode.SETPSR:
                    vm.PSR = op.operand;
                    break;
                case OpCode.HALT: // Sets the HALT bit of the PSR
                    halted = true;
                    Console.WriteLine("\nVM: EXECUTION HALTED");

                    // Run the monitor
                    if (haltBreak)
                    {
                        Console.WriteLine("<ENTERING MONITOR>");
                        monitor();
                    }
                    break;

                // Check instruction:
                case OpCode.CHECK:
                    throw new NotImplementedException("CHECK instruction is not yet implemented in this virtual machine");
                    //break;

                // Character reading and writing:
                case OpCode.CHIN:
                    ConsoleKeyInfo ki = Console.ReadKey();
                    vm.push((ushort)ki.KeyChar); // Technically allows unicode
                    //vm.push((ushort)Console.Read());  
                    break;


                    ////-------------- NON-STANDARD OPCODES HERE! --------------////
                case OpCode.CHOUT:
                    char c = (char) vm.pop();
                    Console.Write(c); // Technically allows unicode
                    break;

                case OpCode.INTIN:
                    string intinLine = Console.ReadLine();
                    short intin = (short) int.Parse(intinLine);
                    vm.push((ushort)intin);
                    break;
                
                case OpCode.INTOUT:
                    short intout = (short)vm.pop();
                    Console.Write(intout);
                    break;

                default:
                    throw new ArgumentOutOfRangeException("Encountered invalid opcode: " + op.opcode);
            }
        }
    }

    public enum OpCode
    {
            NOOP = 0,
            LOADL = 1,
            LOADR = 2,
            LOAD = 3,
            LOADA = 4,
            LOADI = 5,

            STORER = 6,
            STORE = 7,
            STOREI = 8,

            INCR = 9,
            STZ = 10,
            INCREG = 11,

            MOVE = 12,

            SLL = 13,
            SRL = 14,
            ADD = 15,
            SUB = 16,
            MUL = 17,
            DVD = 18,
            DREM = 19,
            LAND = 20,
            LOR = 21,
            INV = 22,
            NEG = 23,

            CLT = 24,
            CLE = 25,
            CEQ = 26,
            CNE = 27,

            BRN = 28,
            BIDX = 29,
            BZE = 30,
            BNZ = 31,
            BNG = 32,
            BPZ = 33,
            BVS = 34,
            BES = 35,

            MARK = 36,
            CALL = 37,
            EXIT = 38,

            SETSP = 39,
            SETPSR = 40,
            HALT = 41,

            CHECK = 42,

            CHIN = 43,
            CHOUT = 44,

            INTIN = 45,
            INTOUT = 46
    }
}
