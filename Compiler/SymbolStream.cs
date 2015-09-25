using System;

namespace Compiler
{
    class SymbolStream
    {
        public struct State
        {
            public SymbolStream Owner { get; }
            public int Line { get; }
            public int Column { get; }
            private Tuple<int, int, int> DefinitionCount { get; }

            public State(SymbolStream owner)
            {
                Owner = owner;
                Line = owner.CurrentString;
                Column = owner.CurrentChar;

                DefinitionCount = owner.CurrentDefinitions.GetCurrentDefinitionCount();
            }

            public void Restore()
            {
                Restore(null);
            }

            public void Restore(string error)
            {
                if (error != null)
                {
                    if (Owner.ErrorMessage == null ||
                        (Owner.CurrentString > Owner.FurthestErrorPosition.Line || 
                        (Owner.CurrentChar >= Owner.FurthestErrorPosition.Column && Owner.CurrentString == Owner.FurthestErrorPosition.Line)))
                    {
                        Owner.FurthestErrorPosition = new State(Owner);
                        Owner.ErrorMessage = error;
                    }
                }

                Owner.CurrentString = Line;
                Owner.CurrentChar = Column;

                Owner.CurrentDefinitions.RestoreDefinitions(DefinitionCount);
            }

            public void RestoreDefinitions()
            {
                Owner.CurrentDefinitions.RestoreDefinitions(DefinitionCount);
            }

            public override string ToString()
            {
                return $"{Owner.LineOrigin[Line]}, column: {Column + 1}";
            }

            public static bool operator >(State a, State b)
            {
                if (a.Line != b.Line)
                    return a.Line > b.Line;
                else
                    return a.Column > b.Column;
            }

            public static bool operator <(State a, State b)
            {
                return b > a;
            }
        }

        public const char EOF = '\0';
        public const char EOL = '\n';
        
        private string[] Lines;
        private string[] LineOrigin;
        private int CurrentString;
        private int CurrentChar;

        private State FurthestErrorPosition;
        private string ErrorMessage;

        public Definitions CurrentDefinitions;

        public SymbolStream(string[] lines, string[] origins)
        {
            CurrentChar = 0;
            CurrentString = 0;

            ErrorMessage = null;

            CurrentDefinitions = new Definitions();
            FurthestErrorPosition = new State(this);

            Lines = lines;
            LineOrigin = origins;
        }

        public bool EndOfFile()
        {
            return CurrentString >= Lines.Length;
        }

        public char Peek()
        {
            if (EndOfFile())
                return EOF;
            else if (CurrentChar == Lines[CurrentString].Length)
                return EOL;
            else
                return Lines[CurrentString][CurrentChar];
        }

        public char Consume()
        {
            char result = Peek();
            if (!EndOfFile())
            {
                CurrentChar++;
                while (CurrentString < Lines.Length && Lines[CurrentString].Length <= CurrentChar)
                {
                    CurrentChar = 0;
                    CurrentString++;
                }
            }

            return result;
        }

        public int SkipWhitespace()
        {
            int count = 0;
            while (true)
            {
                char next = Peek();
                if (next == ' ' || next == '\t' || next == '\n')
                    Consume();
                else
                    return count;

                count++;
            }
        }

        public bool TestNext(char value)
        {
            char peek = Peek();
            if (peek != value)
                return false;
            else
            {
                Consume();
                return true;
            }
        }

        public bool ExpectAndConsume(char value, State restoreTo)
        {
            char peek = Peek();
            if (peek != value)
            {
                restoreTo.Restore($"expected: '{value}', got: '{peek}'");
                return false;
            }
            else
            {
                Consume();
                return true;
            }
        }

        public bool ExpectAndConsumeString(string value, State restoreTo)
        {
            for (int i = 0; i < value.Length; i++)
                if (!ExpectAndConsume(value[i], restoreTo))
                    return false;
            return true;
        }

        public bool ExpectAndConsumeWhitespace(State restoreTo)
        {
            if (SkipWhitespace() == 0)
            {
                restoreTo.Restore($"expected: ' ', got: '{Peek()}'");
                return false;
            }

            return true;
        }

        public string ConsumeToEndOfLine()
        {
            if (CurrentString >= Lines.Length)
                return EOF.ToString();
            else if (CurrentChar == Lines[CurrentString].Length)
                return "";
            else
            {
                string result = Lines[CurrentString].Substring(CurrentChar);
                CurrentChar = 0;
                CurrentString++;
                return result;
            }
        }

        public State SaveState()
        {
            return new State(this);
        }
        
        public string CurrentPosition => $"{LineOrigin[CurrentString]}, column: {CurrentChar + 1}";

        public bool HasError()
        {
            return ErrorMessage != null;
        }

        public void PrintError()
        {
            if (ErrorMessage != null)
            {
                Console.WriteLine($"  {ErrorMessage}");
                Console.WriteLine($"    at {FurthestErrorPosition}");
                Console.WriteLine($"    {Lines[FurthestErrorPosition.Line].Replace('\t', ' ')}");
                Console.Write("    ");
                for (int i = 0; i < FurthestErrorPosition.Column; i++) Console.Write(" ");
                Console.WriteLine("^");
            }
        }
    }
}
