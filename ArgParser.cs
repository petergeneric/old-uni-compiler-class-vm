using System;
using System.Collections;

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

namespace TargetVM.std {
    /// <summary>Eases parsing string argument lists properly & cleanly</summary>
    public class ArgParser {
        /// <summary>Parse an argument list</summary>
        /// <param name="command">an argument string (eg. '1 "2" 3 "4 has a space" ""', '1', '1 2 3', etc.)</param>
        /// <remarks>This is supposed to work for all inputs (and be slightly forgiving). If you find a value that breaks it, please let me know</remarks>
        /// <returns>The individual components</returns>
        public static string[] parse(string command) {
            ArrayList args = new ArrayList(3);
            
            for (int i = 0; i < command.Length; i++) {
                string cparam;
                switch (command[i]) {
                    case '"':
                        int quotePos = command.IndexOf('"', i+1);
                        if (quotePos == -1) {
                            quotePos = command.Length;
                        }
                        cparam = command.Substring(i+1, quotePos - (i+1));

                        args.Add(cparam);
                        i = quotePos; // Skip over all the characters
                        cparam = null;
                        break;
                    case ' ':
                        // Skip empty spaces
                        break;
                    default:

                        int spacePos = command.IndexOf(' ', i + 1);
                        if (spacePos == -1) {
                            spacePos = command.Length;
                        }

                        if (command[i] == ' ') {
                            cparam = command.Substring(i + 1, spacePos - i);
                        }
                        else {
                            cparam = command.Substring(i, spacePos - i);
                        }

                        args.Add(cparam);
                        i = spacePos; // Skip over all the characters
                        cparam = null;
                        break;
                }
            }

            // Now convert the ArrayList to a string array
            return (string[]) args.ToArray(typeof(string));

        }
    }
}