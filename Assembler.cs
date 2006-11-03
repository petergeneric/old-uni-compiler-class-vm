using System;
using System.Text;
using System.Collections;
using System.IO;

namespace TargetVM
{
    class Assembler
    {
        private StreamReader file;

        Core assemblyCore = new Core();

        /// <summary>Assembles a number of source files; each file may contain instructions at any memory location; they are assembled and loaded into memory in the order given</summary>
        /// <param name="asmFiles">A list of Target assembly files</param>
        public Assembler(params String[] asmFiles)
        {
            foreach (String asmFile in asmFiles) {
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
            return assemblyCore.memory;
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
            assemblyCore.memSetWord(addr, operation[0]);
            assemblyCore.memSetWord((ushort)(addr + 1), operation[1]);
        }

        public CpuInstruction assembleInstruction(ushort addr, String instruction, String operands)
        {
            CpuInstruction operation = new CpuInstruction();

            // Now encode the instruction:
            OpCode op = (OpCode)Enum.Parse(typeof(OpCode), instruction.Trim(), true);
            operation.opcode = (byte)op;

            // If we have any operands, pack them into the instruction:
            {
                int[] operandValues = parseOperands(operands);

                operation.setOperandsSmart(operandValues[0], operandValues[2], operandValues[1]);
            }

            return operation;
        }

        public int[] parseOperands(String operands)
        {
            int[] buffer = new int[] {int.MaxValue, int.MaxValue, int.MaxValue };

            if (operands == null || operands.Length == 0)
            {
                return buffer;
            }


            if (char.IsDigit(operands[0]))
            {
#if MONO
                string[] operandArray = split(operands, ',', 2);
#else
                string[] operandArray = operands.Split(new char[] {','}, 2, StringSplitOptions.RemoveEmptyEntries);
#endif
                
                buffer[0] = ushort.Parse(operandArray[0].Trim());

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

            // If the operand contains a comma, it's a register,indirection set

            if (operands.IndexOf(",") != -1)
            {
                // If it starts with a [, strip the square brackets:
                if (operands.StartsWith("[")) {
                    operands = operands.Replace("[", "").Replace("]", "");
                }
#if MONO
                string[] operandArray = split(operands, ',', 2);
#else
                string[] operandArray = operands.Split(new char[] { ',' }, 2, StringSplitOptions.RemoveEmptyEntries);
#endif

                buffer[1] = parseRegister(operandArray[0].Trim());
                buffer[2] = byte.Parse(operandArray[1].Trim());

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
