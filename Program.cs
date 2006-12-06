using System;
using System.Reflection;

// DEFINEables:
//   DOTNET2   - Enables .NET 2.0 features

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
    class Program
    {
        /// <summary>The default value for executing timing (see --time switch)</summary>
        private static bool timeExecution = false;

        public static readonly string assemblyVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();

        [STAThread]
        static unsafe void Main(string[] args)
        {
#if DOTNET2
            Console.Title = "Target Virtual Machine";
#endif

            Console.WriteLine("Target Assembler/Virtual Machine v{0}.", assemblyVersion);
            Console.WriteLine("Copyright (c) 2006, Peter Wright <peter@peterphi.com>");
            Console.WriteLine("");

            Decoder cpu = new Decoder(new Core());

            // Parse any arguments the user has specified on the command-line
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
                            Console.WriteLine("--monitor   Toggles the monitor (default {0})", cpu.debug);
                            Console.WriteLine("--break #   Sets a breakpoint at addr #");
                            Console.WriteLine("--haltBreak Toggles breaking on HALT (default {0})", cpu.haltBreak);
                            Console.WriteLine("--noopBreak Toggles breaking on NOOP (default {0})", cpu.noopBreak);
                            Console.WriteLine("--decode    Toggles monitor's automatic decoding (default {0})", cpu.autoDecode);
                            Console.WriteLine("--echoasm   Toggles assembler's asm echo (default {0})", Assembler.echoAssembledInstructions);
                            Console.WriteLine("--pc #      Sets initial PC value to # (default {0})", cpu.vm.PC);
                            Console.WriteLine("--fp #      Sets initial FP value to # (default {0})", cpu.vm.FP);
                            Console.WriteLine("--sp #      Sets initial SP value to # (default {0})", cpu.vm.SP);
                            Console.WriteLine("--mp #      Sets initial MP value to # (default {0})", cpu.vm.MP);
                            Console.WriteLine("--bp #      Sets initial BP value to # (default {0})", cpu.vm.BP);
                            Console.WriteLine("--time      Toggles performance timing (default {0})", timeExecution);
                            Console.WriteLine("--trace     Saves a trace to file f");
                            Console.WriteLine("--logio     Logs all IO to file f");
                            Console.WriteLine("Option arguments are not error-handled.");

                            return;
                        case "--noopbreak": case "--nb":
                            cpu.noopBreak = !cpu.noopBreak;
                            break;
                        case "--haltbreak":
                        case "--hb":
                            cpu.haltBreak = !cpu.haltBreak;
                            break;
                        case "--pc":
                            cpu.vm.PC = ushort.Parse(args[i + 1]);
                            args[i + 1] = null; // Prevent processing of the next argument
                            break;
                        case "--sp":
                            cpu.vm.SP = ushort.Parse(args[i + 1]);
                            args[i + 1] = null; // Prevent processing of the next argument
                            break;
                        case "--mp":
                            cpu.vm.MP = ushort.Parse(args[i + 1]);
                            args[i + 1] = null; // Prevent processing of the next argument
                            break;
                        case "--fp":
                            cpu.vm.FP = ushort.Parse(args[i + 1]);
                            args[i + 1] = null; // Prevent processing of the next argument
                            break;
                        case "--bp":
                            cpu.vm.BP = ushort.Parse(args[i + 1]);
                            args[i + 1] = null; // Prevent processing of the next argument
                            break;
                        case "--time":
                            timeExecution = !timeExecution;
                            break;
                        case "--monitor":
                        case "--debug": // Display monitor immediately
                            cpu.debug = !cpu.debug;
                            break;
                        case "--logio":
                        case "--lio": // Log IO to disk
                            cpu.startIOLog(args[i + 1]);
                            args[i + 1] = null;
                            break;
                        case "--trace":
                        case "--tracefile": // Save traces to disk
                            cpu.startTrace(args[i+1]);
                            args[i + 1] = null;
                            break;
                        case "--echoasm":
                            Assembler.echoAssembledInstructions = !Assembler.echoAssembledInstructions;
                            break;
                        case "--break": // Enable the monitor, place a breakpoint at the address specified in the next argument:
                            cpu.debug = true;
                            cpu.addrBreak = ushort.Parse(args[i + 1]);
                            args[i + 1] = null; // Prevent processing of the next argument
                            break;
                        
                        default:
                            Console.WriteLine("Unknown argument: " + args[0]);
                            return;
                    }
                }
            }
            #endregion

            // Now assemble the files and dump the result into main memory
            DateTime asmStart = DateTime.Now;
            Assembler a = new Assembler(args);
            DateTime asmStop = DateTime.Now;
            cpu.vm.memory = a.getAssembled();


            Console.WriteLine("\nExecuting code...");

            // Execute until the CPU is halted
            DateTime runStart = DateTime.Now;
            cpu.execute();
            DateTime runStop = DateTime.Now;


            if (timeExecution)
            {
                Console.WriteLine("\nTIMING INFORMATION (+ MONITOR TIME)");
                TimeSpan tAsm = asmStop - asmStart;
                TimeSpan tRun = runStop - runStart;
                Console.WriteLine("  Assembly Duration: {0}", tAsm);
                Console.WriteLine(" Execution Duration: {0}", tRun);
                Console.WriteLine("       Instructions: {0}", cpu.ops);
                Console.WriteLine("        Comparisons: {0}", cpu.cmps);
                Console.WriteLine("            ops/sec: {0}", Math.Round(cpu.ops / tRun.TotalSeconds));
                Console.WriteLine("<PRESS ENTER>");
                Console.ReadLine();
            }

            Console.WriteLine("\nTargetVM: Normal Termination.");
        } // end app
    } // end class
}
