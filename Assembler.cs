#define MONO
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
    class Assembler
    {
        private StreamReader file;
        private ushort[] memory = new ushort[ushort.MaxValue + 1]; // The memory to assemble the file to (allocate 1 extra space to allow for memory[ushort.MaxValue]

        /// <summary>Assembles a number of source files; each file may contain instructions at any memory location; they are assembled and loaded into memory in the order given</summary>
        /// <param name="asmFiles">A list of Target assembly files</param>
        public Assembler(params String[] asmFiles)
        {
            foreach (String asmFile in asmFiles)
            {
                if (asmFile == null) continue; // Skip null files

                // Skip files that don't exist
                if (!File.Exists(asmFile))
                {
                    Console.WriteLine("> Cannot assemble non-existant file: " + asmFile);
                    continue;
                }

                try
                {
                    Console.WriteLine("> Assembling file " + asmFile);
                    file = new StreamReader(asmFile);
                    assemble();
                }
                finally
                {
                    if (file != null)
                    {
                        file.Close();
                    }
                }
            }
        }

        private void assemble()
        {
            string line = "";
            while (line != null)
            {
                line = this.getNextLine();

                if (line != null)
                {
                    assemble(line);
                }
            }

            Console.WriteLine("> Assembled file.");
        }

        public ushort[] getAssembled()
        {
            return memory;
        }

        public string[] split(String str, char delimeter, int count)
        {
            string[] arr = str.Split(new char[] { delimeter });
            ArrayList al = new ArrayList();


            // Now remove empty elements and ensure the maximum count
            for (int i = 0; i < arr.Length; i++)
            {
                if (count != 0)
                {
                    if (arr[i] != null && arr[i].Length != 0)
                    {
                        al.Add(arr[i]);
                        count--;
                    }
                }
                else
                {
                    String s = (String) al[al.Count - 1];
                    s += delimeter + arr[i];
                    al[al.Count - 1] = s;
                }
            }

            return (String[]) al.ToArray(typeof(string));
        }


        /// <summary>Assembles the given instruction line, writing the opcode to the right memory location</summary>
        /// <param name="instruction">The whole instruction line</param>
        public void assemble(string instruction)
        {
            ushort addr;
            string opcode;
            string operand;

#if MONO
            string[] instr = split(instruction, ' ', 3);
#else
            string[] instr = instruction.Split(new char[] { ' ' }, 3, StringSplitOptions.RemoveEmptyEntries);
#endif

            addr = ushort.Parse(instr[0]);
            opcode = instr[1];
            operand = (instr.Length >= 3) ? instr[2] : null;

            CpuInstruction operation = assembleInstruction(addr, opcode, operand);
            Console.WriteLine("{0}:\t{1}", addr, operation.ToString());
            
            // Write the instruction to the assembly core's RAM:
            memory[addr] = operation[0];
            memory[addr + 1] = operation[1];
        }

        public CpuInstruction assembleInstruction(ushort addr, String instruction, String operands)
        {
            CpuInstruction operation = new CpuInstruction();

            // Now encode the instruction:
            OpCode op = (OpCode)Enum.Parse(typeof(OpCode), instruction.Trim(), true);
            operation.opcode = (byte)op;

            // If we have any operands, pack them into the instruction:
            {
                ushort[] operandVals = parseOperands(operands);

                operation.operand = (ushort)operandVals[0];
                operation.register = (byte)operandVals[1];
                operation.indirections = (byte)operandVals[2];
            }

            return operation;
        }


        /// <summary>Parses the operands given to an instruction</summary>
        /// <param name="operands">The operands with NO WHITESPACE ON EITHER SIDE</param>
        /// <returns>The values for the 3 operands</returns>
        public ushort[] parseOperands(String operands)
        {
            ushort[] buffer = new ushort[] {0,0,0};

            if (operands == null || operands.Length == 0)
            {
                return buffer;
            }


            if (char.IsDigit(operands[0]) || operands[0] == '-')
            {
#if MONO
                string[] operandArray = split(operands, ',', 2);
#else
                string[] operandArray = operands.Split(new char[] {','}, 2, StringSplitOptions.RemoveEmptyEntries);
#endif
                
                buffer[0] = (ushort) short.Parse(operandArray[0].Trim());

                // If there are further operands, set the operands
                if (operandArray.Length > 1)
                {
                    operands = operandArray[1];
                }
                else
                {
                    return buffer;
                }
            }

            // If the operand contains a comma, it's a register,value set (either [register, indirections] or register, operand)
            if (operands.IndexOf(",") != -1)
            {
                // If it starts with a [, strip the square brackets:
                if (operands.StartsWith("[")) // FORMAT IS [register, indirections]
                {
                    operands = operands.Replace("[", "").Replace("]", "");
#if MONO
                string[] operandArray = split(operands, ',', 2);
#else
                    string[] operandArray = operands.Split(new char[] { ',' }, 2, StringSplitOptions.RemoveEmptyEntries);
#endif

                    buffer[1] = parseRegister(operandArray[0].Trim());
                    buffer[2] = byte.Parse(operandArray[1].Trim());
                }
                else // OTHERWISE, FORMAT IS: register, operand
                {
#if MONO
                string[] operandArray = split(operands, ',', 2);
#else
                    string[] operandArray = operands.Split(new char[] { ',' }, 2, StringSplitOptions.RemoveEmptyEntries);
#endif

                    buffer[1] = parseRegister(operandArray[0].Trim()); // the register is defined first
                    buffer[0] = ushort.Parse(operandArray[1].Trim()); // second comes the operand
                }

                // We have successfully parsed this operand.
                return buffer;
            }

            // If there are still operands to parse, it must be a single operand on its own
            if (operands != null)
            {
                buffer[1] = parseRegister(operands);

                return buffer;
            }

            Console.WriteLine("ERROR: Invalid operand decoding!");
            return buffer;
        }

        private byte parseRegister(String reg)
        {
            switch (reg)
            {
                case "BP": return 0;
                case "FP": return 1;
                case "MP": return 2;
                case "SP": return 3;
                default:
                    throw new ArgumentOutOfRangeException("Unknown register: " + reg);
            }
        }

        /// <summary>Returns the next assembly line (ignoring blanks and comments)</summary>
        /// <returns></returns>
        public String getNextLine()
        {
            while (true)
            {
                String thisLine = file.ReadLine();

                if (thisLine != null && thisLine.Length > 0 && !thisLine.StartsWith("*")) { // ignore blank and comment lines
                    // If the line starts with a digit, or whitespace and then a digit...
                    if (char.IsDigit(thisLine[0]) || (thisLine.StartsWith(" ") && char.IsDigit(thisLine.Trim()[0])))
                    {
                        // todo: technically we should/could also test for the number on the line being <= 65535
                        return thisLine.Trim();
                    }
                }
                else if (thisLine == null)
                {
                    return null;
                }
            }
        }
    }
}
