using Cecilifier.Runtime;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using System.Collections.Immutable;

namespace CosmosAdvancedEnums {
    internal class Program {
        static Dictionary<string, (MethodDefinition fromstr, MethodDefinition tostr)> generated = new();

        static void Main(string[] args) {
            if(args.Length == 0) {
                Console.WriteLine("Please enter a dll path to attempt to run the postprocessor on");
                return;
            }

            var mod = ModuleDefinition.ReadModule(File.Open(args[0], FileMode.Open));

            foreach (TypeDefinition tdef in mod.Types.ToList()) {
                RunEnumgenPostprocessingOnType(tdef, mod);
            }

            foreach (TypeDefinition tdef in mod.Types.ToList()) {
                RunReplaceCorlibPostprocessingOnType(tdef, mod);
            }

            mod.Write();
        }

        static void RunReplaceCorlibPostprocessingOnType(TypeDefinition tdef, ModuleDefinition mod) {
            Console.Write("Running replacecorlib-postprocessor on " + tdef.FullName);

            foreach(var meth in tdef.Methods) {
                if (!meth.HasBody) continue;

                var ilProc = meth.Body.GetILProcessor();

                foreach(var inst in meth.Body.Instructions.ToList()) {
                    if (inst.OpCode == OpCodes.Constrained && inst.Operand is TypeDefinition opTdef) {
                        if(opTdef.BaseType.FullName == "System.Enum" && inst.Next.OpCode == OpCodes.Callvirt) { // we got a match!
                            var call = ilProc.Create(
                                OpCodes.Call,
                                generated[opTdef.FullName].tostr
                            );

                            ilProc.Replace(inst.Next, call);
                            /*ilProc.InsertAfter(inst.Previous, ilProc.Create(OpCodes.Ldloc, (VariableDefinition)inst.Previous.Operand));
                            ilProc.Remove(inst.Previous.Previous);*/
                            ilProc.Remove(inst);
                        }

                        continue;
                    }

                    if(inst.OpCode == OpCodes.Call && (inst.Operand as MethodReference).Name == "TryParse") {
                        ilProc.Replace(inst, ilProc.Create(OpCodes.Call,
                            generated[(inst.Operand as GenericInstanceMethod).GenericArguments[0].FullName].fromstr));
                    }
                }

                foreach(var attr in meth.CustomAttributes.ToList()) {
                    if(attr.AttributeType.Name == "NullableContextAttribute") {
                        meth.CustomAttributes.Remove(attr);
                    }
                }

                //meth.Body.Optimize();
            }
        }

        static void RunEnumgenPostprocessingOnType(TypeDefinition tdef, ModuleDefinition mod) {
            Console.Write("Running enumgen-postprocessor on " + tdef.FullName + "? ");

            if (tdef.BaseType != null && tdef.BaseType.FullName == "System.Enum") {
                Console.WriteLine("Yes, is enum.");

                #region Check if Int32 base field
                var baseField = tdef.Fields.First((field) => (field.Name == "value__"));
                if (baseField == null) throw new Exception("Unable to find base field of enum " + tdef.BaseType.Name);

                Log(tdef, $"Has base type: Enum<{baseField.FieldType.Name}>");

                if(baseField.FieldType.Name != "Int32") {
                    Log(tdef, $"Aborting post process on type; currently only enums of base type Int32 are supported!");
                    return;
                }
                #endregion

                #region Extract entries
                Log(tdef, $"Extracting entries of enum.");
                var entries = GetEnumEntries(tdef);

                #endregion

                #region Create new class
                Log(tdef, $"Everything looks fine, creating helper class now.");
                var newType = new TypeDefinition(tdef.Namespace, tdef.Name + "Helpers", TypeAttributes.Class | TypeAttributes.Public);
                newType.BaseType = mod.TypeSystem.Object;

                MethodDefinition enumToStringMethodDef, stringToEnumMethodDef;

                // Enum -> String convert
                { // do not question it. yes, it may be ugly. but i do not give a fuck rn
                    enumToStringMethodDef = new MethodDefinition("EnumToString", MethodAttributes.Public | MethodAttributes.Static, GetSystemType<string>(mod));
                    enumToStringMethodDef.Parameters.Add(new("__enum", ParameterAttributes.None, new ByReferenceType(tdef)));
                    enumToStringMethodDef.Body.InitLocals = true;
                    enumToStringMethodDef.Body.Variables.Add(new(GetSystemType<int>(mod)));

                    var ilProc = enumToStringMethodDef.Body.GetILProcessor();
                    ilProc.Append(ilProc.Create(OpCodes.Ldarg_0));
                    ilProc.Append(ilProc.Create(OpCodes.Ldind_I4));
                    ilProc.Append(ilProc.Create(OpCodes.Stloc_0));

                    var excStart = ilProc.Create(OpCodes.Ldstr, "Unexpected enum value");
                    ilProc.Append(excStart);
                    ilProc.Append(ilProc.Create(OpCodes.Newobj, mod.ImportReference(TypeHelpers.ResolveMethod(typeof(System.Exception), ".ctor", System.Reflection.BindingFlags.Default | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public, new string[] { "System.String" }))));
                    ilProc.Append(ilProc.Create(OpCodes.Throw));

                    Instruction nextLdloc = null;

                    foreach (var ent in entries.Reverse()) {
                        var ldloc = ilProc.Create(OpCodes.Ldloc_0);
                        ilProc.InsertAfter(2, ldloc);
                        ilProc.InsertAfter(3, ilProc.Create(OpCodes.Ldc_I4, ent.Value));
                        if (nextLdloc != null) {
                            ilProc.InsertAfter(4, ilProc.Create(OpCodes.Bne_Un, nextLdloc));
                        } else {
                            ilProc.InsertAfter(4, ilProc.Create(OpCodes.Bne_Un, excStart));
                        }
                        ilProc.InsertAfter(5, ilProc.Create(OpCodes.Ldstr, ent.Name));
                        ilProc.InsertAfter(6, ilProc.Create(OpCodes.Ret));

                        nextLdloc = ldloc;
                    }

                    //enumToStringMethodDef.Body.Optimize();
                    newType.Methods.Add(enumToStringMethodDef);
                }

                Log(tdef, "Generated EnumToString method successfully, now doing StringToEnum!");

                // String -> Enum convert
                {
                    stringToEnumMethodDef = new MethodDefinition("StringToEnum", MethodAttributes.Public | MethodAttributes.Static, GetSystemType<bool>(mod));
                    stringToEnumMethodDef.Parameters.Add(new("str", ParameterAttributes.None, GetSystemType<string>(mod)));
                    stringToEnumMethodDef.Parameters.Add(new("__enum", ParameterAttributes.Out, new ByReferenceType(tdef)));

                    stringToEnumMethodDef.Body.InitLocals = true;
                    stringToEnumMethodDef.Body.Variables.Add(new(GetSystemType<int>(mod)));

                    var ilProc = stringToEnumMethodDef.Body.GetILProcessor();
                    ilProc.Append(ilProc.Create(OpCodes.Nop));

                    var excStart = ilProc.Create(OpCodes.Ldc_I4_0);
                    ilProc.Append(excStart);
                    ilProc.Append(ilProc.Create(OpCodes.Ret));

                    Instruction nextLdarg = null;

                    foreach (var ent in entries.Reverse()) {
                        var ldarg = ilProc.Create(OpCodes.Ldarg_0);

                        ilProc.InsertAfter(0, ldarg);
                        ilProc.InsertAfter(1, ilProc.Create(OpCodes.Ldstr, ent.Name));
                        ilProc.InsertAfter(2, ilProc.Create(OpCodes.Call, mod.ImportReference(TypeHelpers.ResolveMethod(typeof(System.String), "op_Equality", System.Reflection.BindingFlags.Default | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public, "System.String", "System.String"))));
                        if (nextLdarg != null) {
                            ilProc.InsertAfter(3, ilProc.Create(OpCodes.Brfalse, nextLdarg));
                        } else {
                            ilProc.InsertAfter(3, ilProc.Create(OpCodes.Brfalse, excStart));
                        }

                        ilProc.InsertAfter(4, ilProc.Create(OpCodes.Ldarg_1));
                        ilProc.InsertAfter(5, ilProc.Create(OpCodes.Ldc_I4, ent.Value));
                        ilProc.InsertAfter(6, ilProc.Create(OpCodes.Stind_I4));
                        ilProc.InsertAfter(7, ilProc.Create(OpCodes.Ldc_I4_1));
                        ilProc.InsertAfter(8, ilProc.Create(OpCodes.Ret));

                        nextLdarg = ldarg;
                    }

                    //stringToEnumMethodDef.Body.Optimize();
                    newType.Methods.Add(stringToEnumMethodDef);
                }

                Log(tdef, "Generated StringToEnum method successfully!");

                generated.Add(tdef.FullName, (stringToEnumMethodDef, enumToStringMethodDef));
                mod.Types.Add(newType);
                #endregion
            } else Console.WriteLine("No, is not enum.");
        }
        
        static ImmutableArray<EnumEntry> GetEnumEntries(TypeDefinition tdef) {
            var entries = new List<EnumEntry>();

            foreach(var field in tdef.Fields) {
                if (field.Attributes.HasFlag(FieldAttributes.SpecialName)) continue; // This is the base type identifying field, irrelevant to us at this point.

                var ent = new EnumEntry() { Name = field.Name, Value = (Int32)field.Constant };
                Log(tdef, $"Found entry with name {ent.Name} (= {ent.Value})");

                entries.Add(ent);
            }

            return entries.ToImmutableArray();
        }

        static TypeDefinition GetSystemType<T>(ModuleDefinition mod) {
            var tr = mod.ImportReference(typeof(T));
            var td = tr.Resolve();
            return td;
        }

        static void Log(TypeDefinition @enum, string str) {
            Console.WriteLine($"  > {@enum.FullName} > " + str);
        }
    }

    struct EnumEntry {
        public string Name { get; set; }
        public int Value { get; set; }
    }
}