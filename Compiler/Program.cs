using System;
using Compiler.Expressions;
using System.Linq;
using System.Collections.Generic;

namespace Compiler
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine($"Cb to assembly compiler");
            Console.Title = "C\u266d compiler";

            bool wait = ShouldWait(args);
            string source = GetSourceFile(args);
            if (source == null)
            {
                Console.WriteLine("missing input file");
                if (wait) Wait();
                return;
            }

            string output = args.SkipWhile(arg => arg != "-o").Skip(1).FirstOrDefault();
            if (output == null)
                output = System.IO.Path.ChangeExtension(source, ".asm");

            try
            {
                string libraryLocation = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().CodeBase).Substring(6) + "\\include\\";

                SymbolStream reader = new Preprocessor(libraryLocation).LoadCode(source);

                reader.SkipWhitespace();

                while (reader.Peek() != SymbolStream.EOF)
                {
                    FunctionDefinition function = FunctionDefinition.TryRead(reader);
                    if (function != null)
                        reader.SkipWhitespace();
                    else
                        throw new CompileException(reader);
                }

                FunctionDefinition main = reader.CurrentDefinitions.FindFunction(new Name("main"));
                if (main == null) throw new CompileException("main function not found");
                if (main.Parameters.Parameters.Length > 0) throw new CompileException("main must take 0 parameters");
                if (main.ReturnType != DataType.Void) throw new CompileException("main must return void");

                //foreach (var function in reader.CurrentDefinitions.DefinedFunctions)
                //    Console.WriteLine(function);

                CodeWriter writer = new CodeWriter();

                var def = new Definitions();
                MarkUsedFunctions(reader.CurrentDefinitions.DefinedFunctions);

                writer.WriteHeader();
                foreach (var function in reader.CurrentDefinitions.DefinedFunctions.Reverse().Where(f => f.ShouldCompile))
                {
                    writer.WriteLine($"; {function}");
                    function.Compile(writer, def);
                    writer.WriteLine();
                }
                writer.WriteLine("END program");

                writer.FlushToFile(output);
                Console.WriteLine($"Output written to '{output}'");
            }
            catch (CompileException e)
            {
                if (e.Stream != null && e.Stream.HasError())
                {
                    e.Stream.PrintError();
                }
                else
                {
                    Console.WriteLine("Compilation error:");
                    Console.WriteLine($"  {e.Message}");
                }
            }

            if (wait) Wait();
        }

        static string GetOutputFile(string[] args)
        {
            int index = -1;
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i].ToLower() == "-o")
                {
                    if (i == args.Length - 1)
                        throw new Exception("missing output file");
                    else if (index != -1)
                        throw new Exception("more than one output file given");
                    else
                        index = i;
                }
            }

            if (index == -1) return null;
            else return args[index + 1];
        }

        static string GetSourceFile(string[] args)
        {
            for (int i = 0; i < args.Length; i++)
                if (!args[i].StartsWith("-") && (i == 0 || args[i - 1].ToLower() != "-o"))
                    return args[i];
            return null;
        }

        static bool ShouldWait(string[] args)
        {
            return args.Any(a => a.ToLower() == "-w");
        }
        
        static void Wait()
        {
            while (Console.KeyAvailable) Console.ReadKey();
            Console.ReadKey();
        }

        static void MarkUsedFunctions(IEnumerable<FunctionDefinition> functions)
        {
            Queue<Name> queuedNames = new Queue<Name>();
            queuedNames.Enqueue(new Name("main"));

            while (queuedNames.Count > 0)
            {
                Name name = queuedNames.Dequeue();
                FunctionDefinition def = functions.FirstOrDefault(f => f.Name == name);
                if (def != null)
                {
                    def.ShouldCompile = true;
                    foreach (var call in def.CallList)
                    {
                        FunctionDefinition c = functions.FirstOrDefault(f => f.Name == call);
                        if (c == null)
                            throw new CompileException($"function {call} is called but not defined");
                        else if (c.Body == null)
                            throw new CompileException($"function {call} is called but not implemented");
                        else if (!c.ShouldCompile)
                            queuedNames.Enqueue(c.Name);
                    }
                }
            }
        }
    }
}
