#define SYSROUTINE_INT_HACK // Ugly hack to side-step the stack corruption by readInt and writeInt sys routines without it being obvious to the user

using System;
using System.Collections;
using System.IO;

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

namespace TargetVM {
    /// <summary>A wrapper around core CPU functionality that allows arbitrary instructions to be executed</summary>
    class Decoder {
        /// <summary>The CPU core</summary>
        public Core vm;

        /// <summary>The current instruction</summary>
        public CpuInstruction op;


        #region Monitor flags
        /// <summary>If set to true, the monitor will appear after the next instruction fetch</summary>
        public bool debug = true;
        /// <summary>Should the monitor be called after execution of a NOOP?</summary>
        public bool noopBreak = true;
        /// <summary>Should the monitor be called after a HALT instruction?</summary>
        public bool haltBreak = true;
        /// <summary>If set to true, automatically prints a decode of the current instruction when the monitor appears</summary>
        public bool autoDecode = true;

        /// <summary>Set to true when any IO operation is performed. Set to false when the monitor runs.</summary>
        public bool ioSinceMonitor = false;

        /// <summary>If != -1, the monitor will not be displayed until we are about to execute the instruction at this address</summary>
        public int addrBreak = -1;
        /// <summary>If nonzero, the monitor will not be displayed until the monitor has been requested this number of times</summary>
        public int monitorSkipInstructions = 0;

        public bool signedAccess = false;
        #endregion

        #region trace / logging flags
        private bool saveTrace = false;
        private StreamWriter traceFile = null;

        private bool ioLog = false;
        private StreamWriter ioFile = null;

        /// <summary>The number of instructions that have been executed</summary>
        public long ops = 0;

        /// <summary>The number of comparison operations that have been executed</summary>
        public long cmps = 0;
        #endregion

        public Decoder(Core vm) {
            this.vm = vm;
        }

        /// <summary>Decodes and executes instructions</summary>
        public unsafe bool execute() {
            while (!vm.psrH) {
                op = vm.getNextInstruction(); // Fetch & decode the next instruction to execute

                // Display the monitor if requested
                if (debug) {
                    monitor();
                }

                // Increment our instruction counter
                ++ops;

                #region Write trace data if necessary
                if (saveTrace) {
                    traceFile.WriteLine("\t\tPSR={0}, FP={1}, SP={2}, MP={3}", vm.PSR, vm.FP, vm.SP, vm.MP);
                    traceFile.Write("{0}\t{1}", (vm.PC - 2), op.ToString());
                }
                #endregion

                #region Instruction Execution
                switch ((OpCode)op.opcode) {
                    case OpCode.NOOP: // "Do Nothing"
                        // Allow the monitor to reassert itself after the next noop
                        if (noopBreak) {
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
                    case OpCode.DREM:
                        vm.mod(); break;
                    case OpCode.INCR: // Increment the top of the stack by operand
                        vm.incr((short)op.operand);
                        break;

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
                        cmps++;
                        short clt2 = (short)vm.pop();
                        short clt1 = (short)vm.pop();
                        if (clt1 < clt2) {
                            vm.push(1);
                        }
                        else {
                            vm.push(0);
                        }
                        break;
                    case OpCode.CLE: // if (pop() <= pop()) push(1) else push(0);
                        cmps++;
                        short cle2 = (short)vm.pop();
                        short cle1 = (short)vm.pop();

                        if (cle1 <= cle2) {
                            vm.push(1);
                        }
                        else {
                            vm.push(0);
                        }
                        break;
                    case OpCode.CEQ: // if (pop() == pop()) push(1) else push(0);
                        cmps++;

                        if (vm.pop() == vm.pop()) {
                            vm.push(1);
                        }
                        else {
                            vm.push(0);
                        }
                        break;
                    case OpCode.CNE: // if (pop() != pop()) push(1) else push(0);
                        cmps++;

                        if (vm.pop() != vm.pop()) {
                            vm.push(1);
                        }
                        else {
                            vm.push(0);
                        }
                        break;

                    // BRANCHING //
                    case OpCode.BRN:
                        vm.PC = op.getOffsetOperand(); break;
                    case OpCode.BIDX:
                        ushort jumpIncrement = vm.pop();
                        ushort increments = op.operand;
                        ushort jumpWords = (ushort)(jumpIncrement * increments);
                        vm.PC += jumpWords;
                        break;
                    case OpCode.BZE: // Branch to m if pop() == 0
                        if (vm.pop() == 0) {
                            vm.PC = op.getOffsetOperand();
                        }
                        break;
                    case OpCode.BNZ: // Branch to m if pop() != 0
                        if (vm.pop() != 0) {
                            vm.PC = op.getOffsetOperand();
                        }
                        break;
                    case OpCode.BNG: // Branch to m if pop() < 0
                        if ((short)vm.pop() < 0) {
                            vm.PC = op.getOffsetOperand();
                        }
                        break;
                    case OpCode.BPZ: // Branch to m if pop() >= 0
                        if ((short)vm.pop() >= 0) {
                            vm.PC = op.getOffsetOperand();
                        }
                        break;
                    case OpCode.BVS: // unknown
                        if (vm.psrV) {
                            vm.psrV = false; // Clear the flag
                            vm.PC = op.getOffsetOperand();
                        }
                        break;
                    case OpCode.BES: // unknown
                        if (vm.psrE) {
                            vm.psrE = false; // Clear the flag
                            vm.PC = op.getOffsetOperand();
                        }
                        break;

                    // SUBROUTINES //
                    case OpCode.MARK: // Set MP to SP and increment SP by m
                        vm.MP = vm.SP;
                        vm.SP += op.operand;
                        break;

                    case OpCode.CALL: // Store FP and PC to the current frame; FP=MP; PC=m
#if SYSROUTINE_INT_HACK
                        // Hack around a bug in the spec: don't CALL the readInt and writeInt system routines (CALLing would corrupt the current frame)

                        ushort jumpTo = op.getOffsetOperand();

                        if (jumpTo == 50 || jumpTo == 100) {
                            if (jumpTo == 50) { // readInt
                                ioSinceMonitor = true;

                                bool valid = false;
                                while (!valid) {
                                    try {
                                        string intinLine = Console.ReadLine();
                                        short intin = (short)int.Parse(intinLine);
                                        vm.push((ushort)intin);
                                        if (ioLog) if (ioLog) ioFile.WriteLine(intin);
                                        valid = true;
                                    }
                                    catch (FormatException) {
                                        Console.WriteLine("(Invalid Number. Try again)");
                                    }
                                }
                            }
                            else { // writeInt
                                ioSinceMonitor = true;
                                short intout = (short)vm.pop();
                                Console.Write(intout);
                                if (ioLog) ioFile.Write(intout);
                                break;
                            }
                        }
                        else { // Normal CALL implementation
                            vm.memory[vm.MP + 1] = vm.FP;
                            vm.memory[vm.MP + 2] = vm.PC;
                            vm.FP = vm.MP;
                            vm.PC = jumpTo;
                        }
#else
                    vm.memory[vm.MP + 1] = vm.FP;
                    vm.memory[vm.MP + 2] = vm.PC;
                    vm.FP = vm.MP;
                    vm.PC = op.getOffsetOperand();
#endif

                        break;

                    case OpCode.EXIT: // Restore FP and PC from the MP stack
                        vm.SP = vm.FP;
                        vm.FP = vm.memory[vm.SP + 1];
                        vm.PC = vm.memory[vm.SP + 2]; // jump back to the caller
                        break;

                    // LOADING //
                    case OpCode.LOADL: // Load a value (operand)
                        vm.push(op.operand); break;
                    case OpCode.LOADR: // Load the value in a register
                        vm.push(vm.getRegister(op.register)); break;
                    case OpCode.LOAD: // Load the value of the offset operand
                        vm.push(vm.memory[op.getOffsetOperand()]); break;
                    case OpCode.LOADA: // Load the address of the offset operand
                        vm.push(op.getOffsetOperand()); break;
                    case OpCode.LOADI: { // Load (operand) words onto the stack, source address on the top of the stack
                            ushort src = vm.pop(); // get the src address
                            ushort srcMax = (ushort)(src + (op.operand));

                            for (; src < srcMax; src += 2) {
                                vm.push(vm.memory[src]);
                            }
                            break;
                        }

                    // STORING //
                    case OpCode.STORER: // Pop to a register
                        vm.setRegister(op.register, vm.pop());
                        break;
                    case OpCode.STORE: // Pop, using m as the destination address
                        vm.memory[op.getOffsetOperand()] = vm.pop();
                        //vm.memSetWord(op.getOffsetOperand(), vm.pop());
                        break;
                    case OpCode.STOREI: { // Pop (operand) words from the stack
                            ushort dest = vm.pop();
                            for (int i = op.operand; i != 0; --i) {
                                vm.memory[dest++] = vm.pop();
                            }
                            break;
                        }

                    case OpCode.STZ: // Store 0 to memory location m
                        vm.memory[op.getOffsetOperand()] = 0;
                        break;
                    case OpCode.INCREG: // Increment register by n
                        vm.incRegister(op.register, op.operand);
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
                        vm.psrH = true;
                        break;

                    // Check instruction:
                    case OpCode.CHECK:
                        vm.SP -= 2;
                        short check1 = (short)vm.memory[vm.SP];
                        short check2 = (short)vm.memory[vm.SP - 1];
                        short check3 = (short)vm.memory[vm.SP + 1];

                        if (check1 <= check2 && check2 <= check3) {
                            vm.psrE = false;
                        }
                        else { // If the PSR[C] bit is set, halt the CPU
                            vm.psrE = true;
                            vm.psrH = vm.psrC;
                        }

                        throw new NotImplementedException("CHECK instruction is not yet implemented in this virtual machine");

                    // Character reading and writing:
                    case OpCode.CHIN:
                        ioSinceMonitor = true;
                        int chinChar = Console.Read();
                        vm.push((ushort)chinChar); // Technically allows unicode
                        if (ioLog) ioFile.Write(chinChar);
                        break;

                    case OpCode.CHOUT:
                        ioSinceMonitor = true;
                        char choutChar = (char)vm.pop();
                        Console.Write(choutChar); // Technically allows unicode
                        if (ioLog) ioFile.Write(choutChar);
                        break;

                    ////-------------- NON-STANDARD OPCODES FOLLOW! --------------////
#if !NOEXTENDEDINSTRUCTIONS
                    case OpCode.BLANK: // Do absolutely nothing (intended as an alternative to NOOP for VM debugging)
                        break;
#endif
                    default:
                        throw new ArgumentOutOfRangeException("Encountered invalid opcode: " + op.opcode);
                } // end switch
                #endregion // Instruction Execution

                // If the CPU has been halted:
                if (vm.psrH) {
                    closeFiles();
                    Console.WriteLine("\n>VM: CPU HALTED");

                    // Run the monitor
                    if (haltBreak) {
                        Console.WriteLine("<ENTERING MONITOR>");
                        monitor();
                    }
                }
            } // while(true)

            return !vm.psrH;
        } // end tick()

        #region Trace & IO log support
        public void startTrace(string fileName) {
            traceFile = new StreamWriter(fileName);
            saveTrace = true;
            traceFile.WriteLine(String.Format("-- TRACE BEGINS {0} {1} --", DateTime.Now.ToLongDateString(), DateTime.Now.ToLongTimeString()));
            traceFile.Write("Initial register values");
        }

        public void startIOLog(string fileName) {
            ioFile = new StreamWriter(fileName);
            ioLog = true;
            if (ioLog) ioFile.WriteLine(String.Format("-- IO LOG BEGINS {0} {1} --", DateTime.Now.ToLongDateString(), DateTime.Now.ToLongTimeString()));
        }

        public void closeFiles() {
            if (saveTrace) {
                traceFile.WriteLine("\t\tPSR={0}, FP={1}, SP={2}, MP={3}", vm.PSR, vm.FP, vm.SP, vm.MP);
                traceFile.WriteLine("-- TRACE ENDS   --");
                traceFile.Flush();
                traceFile.Close();
                traceFile = null;
                saveTrace = false;
            }

            if (ioLog) {
                if (ioLog) ioFile.WriteLine();
                if (ioLog) ioFile.WriteLine(String.Format("-- IO LOG ENDS   {0} {1} --", DateTime.Now.ToLongDateString(), DateTime.Now.ToLongTimeString()));
                ioFile.Flush();
                ioFile.Close();
            }
        }
        #endregion

        #region Monitor Code
        /// <summary>A simple debugging console</summary>
        public unsafe void monitor() {
            #region Monitor disappearance conditions
            if (addrBreak != -1) { // If we have been instructed to break at a specific address:
                if (vm.PC - 2 != addrBreak) {
                    return; // Do not display the monitor yet
                }
                else {
                    addrBreak = -1;
                }
            }
            else if (monitorSkipInstructions != 0) {
                if (--monitorSkipInstructions > 0) {
                    return; // Do not display the monitor yet
                }
            }
            #endregion

            #region Handle coexistance with the UI nicely
            // If the CPU has performed IO since the last execution, ensure we're on a new line
            if (ioSinceMonitor) {
                Console.WriteLine();
                ioSinceMonitor = false;
            }
            #endregion

            // If the user requested it, automatically display the decode of the instruction
            if (autoDecode) {
                Console.WriteLine("\t{1}", (vm.PC - 2), op.ToString());
            }
            // Keep accepting arguments until we receive a terminal command

            #region Monitor switch
            while (true) {
                Console.Write("[{0}] Monitor> ", (vm.PC - 2));
                string[] cmds = TargetVM.std.ArgParser.parse(Console.ReadLine());

                if (cmds.Length == 0) return;

                switch (cmds[0].ToLower()) {
                    case null:
                    case "":
                    case "c":
                    case "continue":
                        return;
                    case "next":
                    case "n":
                        debug = true;
                        return;

                    case "r":
                    case "run":
                        debug = false;
                        return;

                    case "k":
                    case "skip": // Skip n executions:
                        if (cmds.Length == 2) {
                            monitorSkipInstructions = parseAddr(cmds[1]);
                            Console.WriteLine("Skipping monitor for next {0} instruction(s).", monitorSkipInstructions);
                            return;
                        }
                        else {
                            Console.WriteLine("Target Monitor: skip requires an argument. Example: skip 5");
                            break;
                        }

                    case "b":
                    case "break": // Break at a specific address
                        if (cmds.Length == 2) {
                            addrBreak = parseAddr(cmds[1]);
                            Console.WriteLine("Setting breakpoint at address {0}.", addrBreak);
                            return;
                        }
                        else {
                            Console.WriteLine("Target Monitor: break requires an argument. Example: break 210");
                            break;
                        }

                    case "s":
                    case "halt":
                    case "stop":
                    case "exit":
                    case "quit":
                    case "q": // halt execution and terminate
                        closeFiles();

                        Console.WriteLine("Target Monitor: goodbye.");
                        System.Environment.Exit(0);
                        return;
                    case "search": // Search for the occurrances of n in memory
                        if (cmds.Length == 2) {
                            ushort val = parseValue(cmds[1]);

                            Console.WriteLine("Searching memory...");
                            for (int i = vm.memory.Length - 1; i != 0; --i) {
                                if (vm.memory[i] == val) {
                                    Console.WriteLine("{0}: {1}\t('{2}')", i, val, (char)val);
                                }
                            }
                            Console.WriteLine("Complete.");
                        }
                        else {
                            Console.WriteLine("Target Monitor: search requires one argument.");
                        }
                        break;

                    case "hb":
                    case "haltbreak": // examines / sets haltbreak
                        Console.WriteLine("haltBreak={0}", haltBreak);

                        if (cmds.Length == 2) {
                            haltBreak = parseBool(cmds[1]);
                            Console.WriteLine("haltBreak={0}\tCHANGED", haltBreak);
                        }
                        break;

                    case "nb":
                    case "noopbreak": // examines / sets noopBreak
                        Console.WriteLine("noopBreak={0}", noopBreak);

                        if (cmds.Length == 2) {
                            noopBreak = parseBool(cmds[1]);
                            Console.WriteLine("noopBreak={0}\tCHANGED", noopBreak);
                        }
                        break;

                    case "setsp":
                        vm.SP = parseAddr(cmds[1]);
                        Console.WriteLine("SP={0}\tCHANGED", vm.SP);
                        break;

                    case "setfp":
                        vm.FP = parseAddr(cmds[1]);
                        Console.WriteLine("FP={0}\tCHANGED", vm.FP);
                        break;

                    case "setmp":
                        vm.MP = parseAddr(cmds[1]);
                        Console.WriteLine("MP={0}\tCHANGED", vm.MP);
                        break;

                    case "store":
                        if (cmds.Length == 3) {
                            ushort storeAddr = parseAddr(cmds[1]);
                            ushort storeVal = parseValue(cmds[2]);
                            vm.memory[storeAddr] = storeVal;

                            Console.WriteLine("{0}:\t{1}", storeAddr, storeVal);
                        }
                        else {
                            Console.WriteLine("Target Monitor: store takes 2 arguments (address, value)");
                        }
                        break;


                    case "push":
                        ushort data = parseAddr(cmds[1]); ;
                        vm.push(data);
                        Console.WriteLine("Pushed {0} onto the stack.", data);
                        break;

                    case "pop":
                        if (vm.SP > 0) {
                            Console.WriteLine("Popped {0} from the stack.", vm.pop());
                        }
                        else {
                            Console.WriteLine("Stack at top of address space. Cannot pop.");
                        }
                        break;

                    case "j":
                    case "jump":
                        if (cmds.Length == 2) {
                            vm.PC = parseAddr(cmds[1]);
                            Console.WriteLine("Jumping to {0}", vm.PC);
                            this.op = vm.getNextInstruction(); // Decode the instruction and execute it instead
                        }
                        else {
                            Console.WriteLine("Target Monitor: jump requires an argument. Example: jump 50");
                        }
                        break;

                    case "d":
                    case "decode":
                        for (int i = 1; i < cmds.Length; i++) {
                            ushort addr = parseAddr(cmds[i]);
                            CpuInstruction memop = new CpuInstruction();
                            memop[0] = vm.memory[addr];
                            memop[1] = vm.memory[addr + 1];
                            Console.WriteLine("{0}:\t{1}", addr, memop.ToString());
                        }

                        if (cmds.Length == 1) {
                            Console.WriteLine("{0}\t{1}", (vm.PC - 2), op.ToString());
                        }

                        break;
                    case "i":
                    case "inspect":
                        for (int i = 1; i < cmds.Length; i++) {
                            ushort addr = parseAddr(cmds[i]);

                            if (signedAccess) {
                                if (addr < vm.memory.Length) {
                                    Console.WriteLine("{0}:\t0x{1:X4} == {1}s", addr, (short)vm.memory[addr]);
                                }
                                else {
                                    Console.WriteLine("{0}:\tOUT OF BOUNDS");
                                }
                            }
                            else {
                                if (addr < vm.memory.Length) {
                                    Console.WriteLine("{0}:\t0x{1:X4} == {1}u", addr, vm.memory[addr]);
                                }
                                else {
                                    Console.WriteLine("{0}:\tOUT OF BOUNDS");
                                }
                            }

                        }
                        break;
                    case "p":
                    case "peek":
                        ushort pitems = 1;
                        bool peekSigned = signedAccess;
                        if (cmds.Length == 2) {
                            pitems = parseAddr(cmds[1]);
                        }

                        // Check we're not about to read out of the memory bounds
                        if (vm.SP - pitems < 0) {
                            Console.WriteLine("Target Monitor: {0} word{1} back from SP ({2}) is an illegal address.", pitems, (pitems != 1 ? "s" : ""), vm.SP);
                            break;
                        }

                        for (int offset = 1; offset <= pitems; offset++) {
                            ushort w = vm.memory[vm.SP - offset]; // vm.memGetWord(vm.SP - offset);
                            if (peekSigned) {
                                Console.WriteLine("{0}:\t0x{1:X4} == {1}s", vm.SP - offset, (short)w);
                            }
                            else {
                                Console.WriteLine("{0}:\t0x{1:X4} == {1}u", vm.SP - offset, w);
                            }
                        }

                        break;

                    case "g":
                    case "reg":
                    case "registers":
                    case "register":
                    case "calc": // make it easier for the user to think about using reg to calculate values
                        if (cmds.Length == 1) {
                            Hashtable core = vm.coreDump();
                            foreach (string key in core.Keys) {
                                Console.WriteLine("{0}\t= {1}", key, core[key]);
                            }
                        }
                        else {
                            for (int i = 1; i < cmds.Length; i++) {
                                ushort val = parseAddr(cmds[i]);
                                Console.WriteLine("{0} = {1}", cmds[i].ToUpper(), val);
                            }
                        }

                        break;

                    case "assemble":
                    case "asm":
                        if (cmds.Length >= 2) {
                            // Assemble each file passed in as an argument
                            for (int i = 1; i < cmds.Length; i++) {
                                if (File.Exists(cmds[i])) {
                                    Assembler a = new Assembler(vm.memory, cmds[i]);
                                    vm.memory = a.getAssembled(); // technically unnecessary

                                    // Re-decode this instruction (in case it's changed)
                                    vm.PC -= 2;
                                    this.op = vm.getNextInstruction();
                                }
                                else {
                                    Console.WriteLine("Arg #{0} Non-existant file {1}", i, cmds[i]);
                                }
                            }
                        }
                        else {
                            Console.WriteLine("Monitor: assemble requires a parameter. Please quote paths with spaces in them.");
                        }
                        break;
                    case "t":
                    case "trace":
                    case "savetrace":
                        if (cmds.Length == 2) {
                            Console.WriteLine("Monitor: Tracing Enabled");
                            startTrace(cmds[1]);
                        }
                        else {
                            Console.WriteLine("Monitor: trace requires one parameter");
                        }
                        break;
                    case "logio":
                    case "lio":
                    case "io":
                    case "l":
                        if (cmds.Length == 2) {
                            Console.WriteLine("Monitor: IO Logging Enabled");
                            startIOLog(cmds[1].Replace('_', ' '));
                        }
                        else {
                            Console.WriteLine("Monitor: logio requires one parameter. Please quote paths with spaces in them.");
                        }
                        break;
                    case "signed":
                        signedAccess = true;
                        Console.WriteLine("Monitor: memory display set to signed");
                        break;
                    case "unsigned":
                        signedAccess = false;
                        Console.WriteLine("Monitor: memory display set to unsigned");
                        break;

                    case "?":
                    case "help":
#if DOTNET2
                        Console.BackgroundColor = ConsoleColor.Blue;
                        Console.ForegroundColor = ConsoleColor.White;
#endif
                        Console.WriteLine("TARGET MONITOR - QUICK HELP");
#if DOTNET2
                        Console.ResetColor();
#endif
                        Console.WriteLine("Copyright (c) 2006, Peter Wright <peter@peterphi.com>");
                        Console.WriteLine("Commands that take n can understand 'sp', '0,[fp,1]', etc.");
                        Console.WriteLine("d {n}      - Displays a decode of the instruction[s] at n.");
                        Console.WriteLine("             Current instruction displayed if none are");
                        Console.WriteLine("             specified (decode)");
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
                        Console.WriteLine("g          - Displays all registers (registers)");
                        Console.WriteLine("g {n}      - Displays specific register values");
                        Console.WriteLine("calc {n}   - Calculates the address n");
                        Console.WriteLine("asm f      - Assembles file f");
                        Console.WriteLine("t f        - Starts saving trace data to file f");
                        Console.WriteLine("l f        - Logs all IO to file f");
                        Console.WriteLine("signed     - Changes memory display to signed mode");

                        break;

                    default:
                        Console.WriteLine("Monitor: Unknown command");
                        break;
                }
            }
            #endregion
        }

        public ushort parseValue(string value) {
            if (char.IsDigit(value[0])) {
                return (ushort)int.Parse(value);
            }
            else if (value.Length == 3 && value[0] == '\'' && value[2] == '\'') {
                return (ushort)value[1];
            }
            else {
                Console.WriteLine("Target Monitor: Invalid value \"{0}\".", value);
                return 0;
            }
        }


        public bool parseBool(string value) {
            switch (value.ToLower()) {
                case "no":
                case "false":
                case "f":
                case "0": return false;
                case "yes":
                case "true":
                case "t":
                case "1": return true;
                default:
                    return false;
            }
        }

        /// <summary>Smart address parser</summary>
        /// <param name="value">A string representing an address (or a simple address calculation)</param>
        /// <returns>The address associated with that value, or 0 if the string could not be processed</returns>
        public ushort parseAddr(string value) {
            // If the address starts with a -, eat it
            if (value.StartsWith("-")) {
                value = value.Substring(1);
            }

            // expect address,register:
            if (value.StartsWith("[") && value.IndexOf(",") != -1) // expect [address,indirections]
            {
                string[] addrReg = value.Replace("[", "").Replace("]", "").Split(new char[] { ',' }, 2);
                ushort regOffset = parseAddr(addrReg[0]);
                ushort indirections = parseAddr(addrReg[1]);

                // Indirect regOffset by indirections
                for (; indirections != 0; --indirections) {
                    regOffset = vm.memory[regOffset];
                }

                return regOffset;
            }
            else if (value.IndexOf(",") != -1 && value.IndexOf("[") != -1) // expect address,[reg,indirections]
            {
                string[] addrReg = value.Split(new char[] { ',' }, 2);
                ushort baseAddr = parseAddr(addrReg[0]);
                ushort regOffset = parseAddr(addrReg[1]);

                return (ushort)(baseAddr + regOffset);
            }
            else if (value.IndexOf(",") != -1&& char.IsDigit(value[0])) // Technically this allows for 10,10,SP.
            {
                string[] addrReg = value.Split(new char[] { ',' }, 2);
                ushort baseAddr = ushort.Parse(addrReg[0]);
                ushort regOffset = parseAddr(addrReg[1]);

                return (ushort)(baseAddr + regOffset);
            }
            else if (value.IndexOf("+") != -1) // Allow VERY BASIC addition
            {
                string[] addrReg = value.Split(new char[] { '+' }, 2);
                ushort baseAddr = parseAddr(addrReg[0]);
                ushort posOffset = parseAddr(addrReg[1]);

                return (ushort)(baseAddr + posOffset);
            }
            else if (value.IndexOf("-") != -1) // Allow VERY BASIC subtraction
            {
                string[] addrReg = value.Split(new char[] { '-' });
                ushort baseAddr = parseAddr(addrReg[0]);

                for (int i = 1; i < addrReg.Length; i++) {
                    ushort negOffset = parseAddr(addrReg[1]);
                    baseAddr = (ushort)(baseAddr - negOffset);
                }

                return baseAddr;
            }

            if (value.Length > 0) {
                switch (value.ToLower()) {
                    case "sp":
                        return vm.SP;
                    case "bp":
                        return vm.BP;
                    case "mp":
                        return vm.MP;
                    case "fp":
                        return vm.FP;
                    case "pc":
                        return vm.PC;

                    // SPECIAL CASE: gives the currently executing instruction
                    case "instr":
                    case "pc--":
                    case "pc-=2":
                    case "here":
                        if (vm.PC >= 2) {
                            return (ushort)(vm.PC - 2);
                        }
                        else {
                            return 0;
                        }
                    default:
                        if (char.IsDigit(value[0])) {
                            try {
                                return (ushort)ushort.Parse(value);
                            }
                            catch (Exception) {
                                Console.WriteLine("[Cannot parse address {0}]", value);
                                return 0;
                            }
                        }
                        else {
                            Console.WriteLine("[Cannot parse address {0}]", value);
                            return 0;
                        }
                }
            }
            else {
                Console.WriteLine("[Cannot parse address {0}]", value);
                return 0;
            }
        }

        #endregion
    }
}
