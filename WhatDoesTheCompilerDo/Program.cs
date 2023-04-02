namespace WhatDoesTheCompilerDo {
    internal class Program {
        static void Main(string[] args) {
            Console.WriteLine("Hello, World!");
        }
    }

    enum Bleh {
        No,
        Yes,
        Ok,
    }

    class BlehHelpers {
        public static string EnumToString(ref Bleh __enum) {
            var v = (int)__enum;

            if(v == 1) {
                return "Yes";
            }else if(v == 2) {
                return "Ok";
            }

            throw new Exception();
        }
    }
}