namespace WhatDoesTheCompilerDo {
    internal class Program {
        static void Main(string[] args) {
            Enum.TryParse<Bleh>("no", true, out Bleh bleh);
            Enum.TryParse<Bleh>("no", out Bleh bleh2);

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

            if (v == 1) {
                return "Yes";
            } else if (v == 2) {
                return "Ok";
            }

            throw new Exception();
        }

        public static bool StringToEnum(string str, out Bleh bleh) {
            if(str.Equals("No", StringComparison.OrdinalIgnoreCase)) {
                bleh = Bleh.No;
                return true;
            } else if (str.Equals("Yes", StringComparison.OrdinalIgnoreCase)) {
                bleh = Bleh.Yes;
                return true;
            } else if (str.Equals("Ok", StringComparison.OrdinalIgnoreCase)) {
                bleh = Bleh.Ok;
                return true;
            }

            bleh = Bleh.No;
            return false;
        }

        public static bool StringToEnumIgnoreCase(string str, out Bleh bleh) {
            str = str.ToLowerInvariant();

            if (str.Equals("No", StringComparison.OrdinalIgnoreCase)) {
                bleh = Bleh.No;
                return true;
            } else if (str.Equals("Yes", StringComparison.OrdinalIgnoreCase)) {
                bleh = Bleh.Yes;
                return true;
            } else if (str.Equals("Ok", StringComparison.OrdinalIgnoreCase)) {
                bleh = Bleh.Ok;
                return true;
            }

            bleh = Bleh.No;
            return false;
        }


    }
}