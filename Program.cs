using System;
using System.Reflection;

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
    class Program
    {
        public static readonly string assemblyVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();

        private static bool timeExecution = false;

        [STAThread]
        static void Main(string[] args)
        {
#if !DOTNET1
            Console.Title = "Target Virtual Machine";
#endif

            Console.WriteLine("Target Assembler/Virtual Machine v{0}.", assemblyVersion);
            Console.WriteLine("Copyright (c) 2006, Peter Wright <peter@peterphi.com>");
            Console.WriteLine("");

            Core cpu = new Core();
            Decoder decoder = new Decoder(cpu);

            // Now parse any arguments the user has specified on the command-line
            #region argument parsing
            for (int i=0;i<args.Length;i++) {
                if (args[i] != null && args[i].StartsWith("--"))
                {
                    string arg = args[i].ToLower();
                    args[i] = null; // Prevent the assembler from processing this instruction

                    switch (arg)
                    {
                        case "--help":
                        case "--usage":
                            Console.WriteLine("Usage:");
                            Console.WriteLine("vm.exe {options} objectCodeFile {objectCodeFile}");
                            Console.WriteLine("Options:");
                            Console.WriteLine("--monitor   Toggles the monitor (default {0})", decoder.debug);
                            Console.WriteLine("--break #   Sets a breakpoint at addr #");
                            Console.WriteLine("--haltBreak Toggles breaking on HALT (default {0})", decoder.haltBreak);
                            Console.WriteLine("--noopBreak Toggles breaking on NOOP (default {0})", decoder.noopBreak);
                            Console.WriteLine("--decode    Toggles monitor's automatic decoding (default {0})", decoder.autoDecode);
                            Console.WriteLine("--echoasm   Toggles assembler's asm echo (default {0})", Assembler.echoAssembledInstructions);
                            Console.WriteLine("--pc #      Sets initial PC value to # (default {0})", decoder.vm.PC);
                            Console.WriteLine("--fp #      Sets initial FP value to # (default {0})", decoder.vm.FP);
                            Console.WriteLine("--sp #      Sets initial SP value to # (default {0})", decoder.vm.SP);
                            Console.WriteLine("--mp #      Sets initial MP value to # (default {0})", decoder.vm.MP);
                            Console.WriteLine("--bp #      Sets initial BP value to # (default {0})", decoder.vm.BP);
                            Console.WriteLine("--time      Toggles performance timing (default {0})", timeExecution);
                            Console.WriteLine("--trace     Saves a trace to file f");
                            Console.WriteLine("--logio     Logs all IO to file f");
                            Console.WriteLine("Option arguments are not error-handled.");

                            return;
                        case "--noopbreak": case "--nb":
                            decoder.noopBreak = !decoder.noopBreak;
                            break;
                        case "--haltbreak":
                        case "--hb":
                            decoder.haltBreak = !decoder.haltBreak;
                            break;
                        case "--pc":
                            decoder.vm.PC = ushort.Parse(args[i + 1]);
                            args[i + 1] = null; // Prevent processing of the next argument
                            break;
                        case "--sp":
                            decoder.vm.SP = ushort.Parse(args[i + 1]);
                            args[i + 1] = null; // Prevent processing of the next argument
                            break;
                        case "--mp":
                            decoder.vm.MP = ushort.Parse(args[i + 1]);
                            args[i + 1] = null; // Prevent processing of the next argument
                            break;
                        case "--fp":
                            decoder.vm.FP = ushort.Parse(args[i + 1]);
                            args[i + 1] = null; // Prevent processing of the next argument
                            break;
                        case "--bp":
                            decoder.vm.BP = ushort.Parse(args[i + 1]);
                            args[i + 1] = null; // Prevent processing of the next argument
                            break;
                        case "--time":
                            timeExecution = !timeExecution;
                            break;
                        case "--monitor":
                        case "--debug": // Display monitor immediately
                            decoder.debug = !decoder.debug;
                            break;
                        case "--logio":
                        case "--lio": // Log IO to disk
                            decoder.startIOLog(args[i + 1]);
                            args[i + 1] = null;
                            break;
                        case "--trace":
                        case "--tracefile": // Save traces to disk
                            decoder.startTrace(args[i+1]);
                            args[i + 1] = null;
                            break;
                        case "--echoasm":
                            Assembler.echoAssembledInstructions = !Assembler.echoAssembledInstructions;
                            break;
                        case "--break": // Enable the monitor, place a breakpoint at the address specified in the next argument:
                            decoder.debug = true;
                            decoder.addrBreak = ushort.Parse(args[i + 1]);
                            args[i + 1] = null; // Prevent processing of the next argument
                            break;
                        
                        default:
                            Console.WriteLine("Unknown argument: " + args[0]);
                            return;
                    }
                }
            }
            #endregion

            DateTime asmStart = DateTime.Now;

            // Now assemble the files and dump the result into main memory
            Assembler a = new Assembler(args);
            DateTime asmStop = DateTime.Now;
            cpu.memory = a.getAssembled();

            Console.WriteLine("\nStarting CPU...");
            DateTime runStart = DateTime.Now;
            // Execute until the CPU is halted
            while (!decoder.halted)
            {
                decoder.tick();
            }
            DateTime runStop = DateTime.Now;

            if (timeExecution)
            {
                Console.WriteLine("\nTIMING INFORMATION (INCL. MONITOR TIME)");
                TimeSpan tAsm = asmStop - asmStart;
                TimeSpan tRun = runStop - runStart;
                Console.WriteLine("    Assembly Duration: {0}", tAsm);
                Console.WriteLine("   Execution Duration: {0}", tRun);
                Console.WriteLine("Instructions executed: {0}", decoder.executedInstructions);
                Console.WriteLine("      average ops/sec: {0}", ((double)decoder.executedInstructions / tRun.TotalSeconds));
                Console.WriteLine("<PRESS ENTER>");
                Console.ReadLine();

            }

            Console.WriteLine("\nTargetVM: Normal Termination.");
        } // end app
    } // end class
}
