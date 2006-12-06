using System;

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
    /// <summary>A strictly-typed enumeration of the op-codes in the Target instruction set</summary>
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

        // NON-STANDARD TARGET INSTRUCTIONS //
#if !NOEXTENDEDINSTRUCTIONS
        BLANK = 47 // BLANK instruction (guaranteed no-operation)
#endif
    }
}
