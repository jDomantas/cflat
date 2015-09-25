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
            WriteLine(".STACK 7FFFh");
            WriteLine(".DATA");
            IncreaseIdentationLevel();
            WriteLine("__firstItemAddress dw 2");
            WriteLine("__firstBlockNext dw 0");
            WriteLine("__firstBlockSize dw 16384");
            WriteLine("__firstBlockPayload db 16380 dup(0)");
            DecreaseIdentationLevel();
            WriteLine();
            WriteLine(".CODE");
            WriteLine();
            WriteLine("program:");
            IncreaseIdentationLevel();
            WriteLine("; init data segment");
            WriteLine("mov ax, @data");
            WriteLine("mov ds, ax");
            WriteLine("; call main");
            WriteLine("call main");
            WriteLine("mov al, 0");
            WriteLine("mov ah, 4Ch");
            WriteLine("int 21h");
            DecreaseIdentationLevel();
            WriteLine();
            WriteUtilityMacros();
            WriteLine();
            WriteLine();
        }

        public void WriteUtilityMacros()
        {
            WriteLine("; utility macros");
            WriteLine("push_byte macro val");
            IncreaseIdentationLevel();
            WriteLine("sub sp, 1");
            WriteLine("mov si, sp");
            WriteLine("mov byte ptr ss:[si], val");
            DecreaseIdentationLevel();
            WriteLine("endm");
            WriteLine();
            WriteLine("pop_byte macro to");
            IncreaseIdentationLevel();
            WriteLine("mov si, sp");
            WriteLine("mov to, byte ptr ss:[si]");
            WriteLine("add sp, 1");
            DecreaseIdentationLevel();
            WriteLine("endm");
            WriteLine();
            WriteLine("pop_bytes macro count");
            IncreaseIdentationLevel();
            WriteLine("add sp, count");
            DecreaseIdentationLevel();
            WriteLine("endm");
        }
    }
}
