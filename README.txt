Target Virtual Machine
Documentation
======================

Copyright (c) 2006, Peter Wright <peter@peterphi.com>

OVERVIEW
This application is a powerful command-line assembler and emulator for the Target architecture.

REQUIREMENTS
A .NET 2-compatible runtime (Windows users can obtain the Microsoft .NET Framework 2.0 from Windows Update). Linux and OS X users should use Mono (available at www.mono-project.com)

The CPU and Memory requirements of the application are low: memory usage should always be < 10MB

PLATFORM SUPPORT
This application is written in C#, and requires a .NET Framework 2.0 (or compatible) environment. Supported environments:
    - Microsoft .NET Framework 2.0 under Windows XP
    - Mono 1.1.18 under Mac OS X (Intel)

While not been tested, x86-64 architectures should work

SOURCE CODE
The source is available under a BSD license.
When compiling, note that the following DEFINE compiler flags are available:
	MONO	- Enables workarounds for Mono class library bugs
	DOTNET	- Enables Windows and Microsoft .NET-specific features
	DOBEEP	- Enables support for beeps when the monitor appears
	NOEXTENDEDINSTRUCTIONS - Forces a strictly spec-compliant virtual machine

Mono users should remember to use the gmcs compiler.

BUGS
While the software has been tested extensively, bugs may still exist; if you find any, please e-mail them to Peter Wright <peter@peterphi.com> (feel free to submit a fix!)


HOW TO USE THE APPLICATION
The application must be launched from the command-line. To do this, open a command shell:
OS X & Linux	- Your preferred shell
Windows		- Start/Run "cmd"  then type cd "C:\Path\To\Executable"

Depending on the platform and .NET machine being used:
Mono	mono vm.exe /path/to/MyProgram.cod /path/to/SystemRoutines.cod
.NET	vm.exe C:\Path\To\MyProgram.cod C:\Path\To\SystemRoutines.cod

COMMAND-LINE ARGUMENTS
The application supports a number of command-line switches to modify its default behaviour. run "vm.exe --help" to list these options

ASSEMBLER
As provided, the application assembles source files. It would be trivial to extend it to provide support for loading preassembled binaries.

The assembler understands the output of the Model compiler; it does not understand tab characters.
Instructions do not have to be specified in memory order.
Lines beginning with (optional whitespace and then) a * character are considered comments, and will be ignored. Blank lines will also be ignored.

VIRTUAL MACHINE
The virtual machine is the main part of the application. It is pretty fast (about 10M complex instructions/second on a 3GHz Intel Core 2 Duo processor) and includes a monitor to examine the machine's state as it executes.



INTRODUCING THE MONITOR
By default, the monitor appears after the files have been assembled. Its prompt looks like this:
	SETPSR 0
[0] Monitor>

The first line is a disassembly of the instruction that is about to be executed.
On the second line, the number in square brackets is the address of that instruction.

The monitor supports an extensive list of commands. A quick list is available by typing "help".

The main commands are:
Pressing the enter key executes a single instruction.
"quit" will terminate the application immediately
"reg" will list the values of the various CPU registers.
"run" will run the program, by default returning to the monitor (breaking) when a NOOP is executed or the CPU is halted.
"peek #" will display the top # values on the stack
"break #" sets a breakpoint at address # and runs the program
"decode #" will decode the instruction starting at address #.
"calc" will perform a calculation. See THE MONITOR: ADDRESSES for more details
"inspect" or "i" will display the value stored at a given address; See THE MONITOR: ADDRESSES for more details

"skip #" will run the program for # instructions, then return to the monitor



THE MONITOR: ADVANCED COMMANDS
The monitor also allows the state of the machine to be altered. This includes:
Modifying registers:
	"setFP", "setMP", "setSP" will set the registers in the machine
Modifying the stack:
	"push #" and "pop" will push/pop things onto/off the stack
Jumping:
	"jump #" will jump execution immediately to the specified address
Memory alteration:
	"store #1 #2" will store #2 to memory location #1

In addition, the "search #" command enables a search of the machine's memory for a specific value. search also accepts character constants in the form 'x'


THE MONITOR: ADDRESSES
To aid inspection, the monitor has a rich address parser. It can understand the following types of addresses:
123		=	123
PC		=	The current value of the Program Counter
here		=	The address of the current instruction
SP-2		=	2 words back from the stack pointer
3,[FP,1]	=	As in Target assembly. Note: no spaces!
[FP,0]		=	Identical to 0,[FP,0] in target assembly
SP+PC		=	Stack Pointer added to Program Counter


Most commands that take an address can display the value at that address as a signed or unsigned value; by default, numbers are displayed unsigned. To display an address as a signed value, prepend it with a "-" sign (eg. "i -10 20 -30" will show the signed value of address 10 and 30, but the unsigned value of address 20)

Finally, it's worth remembering that most monitor commands that take an address can take multiple addresses: for example: "i sp-1 sp-2 sp-3" would display the top 3 items on the stack (although "peek 3" does the same thing)
