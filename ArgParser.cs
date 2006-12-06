using System;
using System.Collections;

namespace TargetVM.std {
    public class ArgParser {
        /// <summary>Parse an argument list</summary>
        /// <param name="command">an argument string (eg. '1 "2" 3 "4 has a space" ""', '1', '1 2 3', etc.)</param>
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
                        i = quotePos;
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
                        i = spacePos;
                        cparam = null;
                        break;
                }
            }

            return (string[]) args.ToArray(typeof(string));

        }
    }
}