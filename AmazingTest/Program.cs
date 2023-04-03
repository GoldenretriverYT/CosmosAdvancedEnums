namespace AmazingTest {
    internal class Program {
        static void Main(string[] args) {
            Console.WriteLine("Enter a token type to parse:");
            Console.WriteLine("ex. " + TokenType.Plus.ToString());

            var str = Console.ReadLine() ?? throw new Exception("oof!");

            if(!Enum.TryParse<TokenType>(str, true, out var res)) {
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

    enum MyNewEnum {
        Ok,
        yeah,
        cool,
        shitandthat = 69
    }
}