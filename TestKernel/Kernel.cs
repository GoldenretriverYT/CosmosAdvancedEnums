using System;
using System.Collections.Generic;
using System.Text;
using Sys = Cosmos.System;

namespace TestKernel {
    public class Kernel : Sys.Kernel {

        protected override void BeforeRun() {
            Console.WriteLine("Cosmos booted successfully. Type a line of text to get it echoed back.");
        }

        protected override void Run() {
            Console.WriteLine("Enter a token type to parse:");
            Console.WriteLine("ex. " + TokenType.Plus.ToString());

            var str = Console.ReadLine() ?? throw new Exception("oof!");

            if (!Enum.TryParse<TokenType>(str, out var res)) {
                Console.WriteLine("nope, not correct.");
                return;
            }

            Console.WriteLine(res.ToString());
        }
    }

    enum TokenType {
        Plus,
        Minus,
        Star,
        Slash
    }
}
