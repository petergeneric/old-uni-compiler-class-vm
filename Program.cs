using System;
using System.Text;

namespace TargetVM
{
    
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Assembler a = new Assembler("sysroutines.cod.txt", "code.cod.txt");

            ushort[] memory = a.getAssembled();

            Core cpu = new Core();
            cpu.memory = memory;



            Decoder decoder = new Decoder(cpu);
            while (!decoder.halted)
            {
                decoder.tick();
            }

        } // end app
    } // end class
}
