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
            WriteLine(".MODEL tiny");
            WriteLine(".STACK 4000h");
            WriteLine(".DATA");
            IncreaseIdentationLevel();
            WriteLine("__firstItemAddress dw 2");
            WriteLine("__firstBlockNext dw 0");
            WriteLine("__firstBlockSize dw 4000h");
            WriteLine("__firstBlockPayload db 3FFCh dup(?)");
            DecreaseIdentationLevel();
            WriteLine();
            WriteLine(".CODE");
            WriteLine("program:");
            IncreaseIdentationLevel();
            WriteLine("; init data segment");
            WriteLine("mov ax, @data");
            WriteLine("mov ds, ax");
            WriteLine("; call main");
            WriteLine("call main");
            WriteLine("; exit(0)");
            WriteLine("mov al, 0");
            WriteLine("mov ah, 4Ch");
            WriteLine("int 21h");
            DecreaseIdentationLevel();
            WriteLine();
        }
    }
}
