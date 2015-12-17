using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Compiler
{
    class CodeWriter
    {
        private List<string> Text;
        private int IdentationLevel;

        public CodeWriter()
        {
            Text = new List<string>();
            IdentationLevel = 0;
        }

        public void FlushToFile(string file)
        {
            try
            {
                File.WriteAllLines(file, Text);
            } catch (System.Exception)
            {
                throw new CompileException($"can't write to file: '{file}'");
            }
        }
        
        public void WriteLine(string value)
        {
            // if (value.TrimStart().StartsWith(";")) return;

            StringBuilder builder = new StringBuilder(value.Length + IdentationLevel * 2);
            for (int i = 0; i < IdentationLevel; i++)
                builder.Append("    ");
            builder.Append(value);

            Text.Add(builder.ToString());
        }

        public void WriteLine()
        {
            Text.Add("");
        }

        public void WriteLines(IEnumerable<string> lines)
        {
            foreach (var line in lines)
                WriteLine(line);
        }
        
        public void IncreaseIdentationLevel()
        {
            IdentationLevel++;
        }

        public void DecreaseIdentationLevel()
        {
            IdentationLevel--;
        }
        
        public void WriteHeader()
        {
            WriteLine(".MODEL compact");
            WriteLine();
            WriteLine(".CODE");
            WriteLine("program:");
            IncreaseIdentationLevel();
            WriteLine("; init data and stack segments");
            WriteLine("mov ax, @data");
            WriteLine("mov ds, ax");
            WriteLine("mov ss, ax");
            WriteLine("mov sp, 0FFFEh");
            WriteLine("; call main");
            WriteLine("call main");
            WriteLine("; exit(0)");
            WriteLine("mov al, 0");
            WriteLine("mov ah, 4Ch");
            WriteLine("int 21h");
            DecreaseIdentationLevel();
            WriteLine();
        }

        public void WriteEnd()
        {
            WriteLine("END program");
            WriteLine();
            WriteLine(".DATA");
            IncreaseIdentationLevel();
            WriteLine("__firstItemAddress dw 2");
            WriteLine("__firstBlockNext dw 0");
            WriteLine("__firstBlockSize dw 7FFEh");
            WriteLine("__firstBlockPayload db 7FFAh dup(?)");
            WriteLine();
            WriteLine("__stackPayload1 db 4000h dup(?)");
            WriteLine("__stackPayload2 db 4000h dup(?)");
            DecreaseIdentationLevel();

        }
    }
}
