using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Compiler
{
    class Preprocessor
    {
        private struct PreprocessorIf
        {
            public bool IsValid { get; }
            public bool HasElse { get; set; }

            public PreprocessorIf(bool valid)
            {
                IsValid = valid;
                HasElse = false;
            }
        }

        private Dictionary<string, string> Definitions;
        private string LibraryLocation;

        public Preprocessor(string libraryLocation)
        {
            Definitions = new Dictionary<string, string>();
            LibraryLocation = libraryLocation;
        }

        public SymbolStream LoadCode(string filename)
        {
            Definitions.Clear();

            var data = CutComments(ScanFile(filename)).ToArray();
            string[] lines = data.Select(t => t.Item1).ToArray();
            string[] origin = data.Select(t => t.Item2).ToArray();

            return new SymbolStream(lines, origin);
        }

        private IEnumerable<Tuple<string, string>> CutComments(IEnumerable<Tuple<string, string>> lines)
        {
            foreach (var t in lines)
            {
                // t.Item1 - original line
                // t.Item2 - origin

                int comment = FindComment(t.Item1);
                if (comment != -1)
                    yield return Tuple.Create(t.Item1.Substring(0, comment), t.Item2);
                else
                    yield return t;
            }
        }
       
        private IEnumerable<Tuple<string, string>> ScanFile(string filename)
        {
            Console.WriteLine($"Scanning file {System.IO.Path.GetFileName(filename)}");

            FileStream file;
            StreamReader reader;

            try
            {
                file = File.Open(filename, FileMode.Open);
                reader = new StreamReader(file);
            }
            catch (Exception)
            {
                throw new CompileException($"can't open file: {filename}");
            }

            string line;
            int lineNumber = 0;

            Stack<PreprocessorIf> ifs = new Stack<PreprocessorIf>();

            while (!reader.EndOfStream)
            {
                lineNumber++;
                line = reader.ReadLine();
                if (line.TrimStart().StartsWith("#"))
                {
                    string op = line.TrimStart().Substring(1).Trim();
                    if (op.StartsWith("include"))
                    {
                        if (ifs.Count == 0 || (ifs.Peek().IsValid ^ ifs.Peek().HasElse))
                        {
                            op = op.Substring(7).Trim();
                            if (op.StartsWith("\"") && op.EndsWith("\""))
                            {
                                foreach (var l in ScanFile(op.Substring(1, op.Length - 2).Trim()))
                                    yield return l;
                            }
                            else if (op.StartsWith("<") && op.EndsWith(">"))
                            {
                                foreach (var l in ScanFile(LibraryLocation + op.Substring(1, op.Length - 2).Trim() + ".cb"))
                                    yield return l;
                            }
                            else
                                throw new CompileException($"invalid include: {op}", $"file: {filename}, line: {lineNumber}");
                        }
                    }
                    else if (op.StartsWith("ifdef") && char.IsWhiteSpace(op[5]))
                    {
                        string check = op.Substring(6).Trim();
                        if (!IsValidDefinition(check))
                            throw new CompileException($"invalid definition: {check}", $"file: {filename}, line: {lineNumber}");

                        if (!Definitions.ContainsKey(check))
                            ifs.Push(new PreprocessorIf(false));
                        else
                            ifs.Push(new PreprocessorIf(ifs.Count == 0 || ifs.Peek().IsValid));
                    }
                    else if (op.StartsWith("ifndef") && char.IsWhiteSpace(op[6]))
                    {
                        string check = op.Substring(6).Trim();
                        if (!IsValidDefinition(check))
                            throw new CompileException($"invalid definition: {check}", $"file: {filename}, line: {lineNumber}");

                        if (Definitions.ContainsKey(check))
                            ifs.Push(new PreprocessorIf(false));
                        else
                            ifs.Push(new PreprocessorIf(ifs.Count == 0 || ifs.Peek().IsValid));
                    }
                    else if (op == "else")
                    {
                        if (ifs.Count > 0 && !ifs.Peek().HasElse)
                        {
                            var currIf = ifs.Pop();
                            currIf.HasElse = true;
                            ifs.Push(currIf);
                        }
                        else if (ifs.Count > 0)
                        {
                            throw new CompileException($"current if already has else directive", $"file: {filename}, line: {lineNumber}");
                        }
                        else
                        {
                            throw new CompileException($"else directive without corresponding if", $"file: {filename}, line: {lineNumber}");
                        }
                    }
                    else if (op == "endif")
                    {
                        if (ifs.Count > 0)
                            ifs.Pop();
                        else
                            throw new CompileException($"endif directive without corresponding if", $"file: {filename}, line: {lineNumber}");
                    }
                    else if (op.StartsWith("define") && char.IsWhiteSpace(op[6]))
                    {
                        if (ifs.Count == 0 || (ifs.Peek().IsValid ^ ifs.Peek().HasElse))
                        {
                            string constant = op.Substring(7).Trim();
                            int firstSpace = constant.IndexOf(' ');
                            if (firstSpace == -1) firstSpace = constant.IndexOf('\t');

                            string value;

                            if (firstSpace == -1)
                                value = "1";
                            else
                            {
                                value = constant.Substring(firstSpace + 1).Trim();
                                constant = constant.Substring(0, firstSpace);
                            }

                            if (!IsValidDefinition(constant))
                                throw new CompileException($"invalid definition: {constant}", $"file: {filename}, line: {lineNumber}");

                            if (Definitions.ContainsKey(constant))
                                throw new CompileException($"redefinition of {constant}", $"file: {filename}, line: {lineNumber}");

                            Definitions.Add(constant, value);
                        }
                    }
                    else if (op.StartsWith("undef") && char.IsWhiteSpace(op[5]))
                    {
                        if (ifs.Count == 0 || (ifs.Peek().IsValid ^ ifs.Peek().HasElse))
                        {
                            string constant = op.Substring(6).Trim();
                            if (!Definitions.ContainsKey(constant))
                                throw new CompileException($"constant is not defined: {constant}", $"file: {filename}, line: {lineNumber}");

                            Definitions.Remove(constant);
                        }
                    }
                    else
                    {
                        throw new CompileException($"unknown preprocessor directive: #{op}", $"file: {filename}, line: {lineNumber}");
                    }
                }
                else
                {
                    if (ifs.Count == 0 || (ifs.Peek().IsValid ^ ifs.Peek().HasElse))
                        yield return Tuple.Create(ReplaceDefinitions(line), $"file: {filename}, line: {lineNumber}");
                }
            }

            if (ifs.Count > 0)
                throw new CompileException($"open if directive", $"file: {filename}");

            reader.Close();
            file.Close();
        }
        
        private string ReplaceDefinitions(string line)
        {
            bool inString = false, inChar = false;
            for (int i = 0; i < line.Length; i++)
            {
                if (inChar)
                    inChar ^= (line[i] == '\'');
                else if (inString)
                    inString ^= (line[i] == '"');
                else
                {
                    if (line[i] == '\'') inChar = true;
                    else if (line[i] == '"') inString = true;
                    else if ((char.IsLetter(line[i]) || line[i] == '_') &&
                        (i == 0 || !(char.IsLetterOrDigit(line[i - 1]) || line[i - 1] == '_')))
                    {
                        string key = Definitions.FirstOrDefault(p =>
                        {
                            string k = p.Key;
                            if (line.Length - i < k.Length) return false;
                            string sub = line.Substring(i);
                            if (!sub.StartsWith(k)) return false;
                            return sub.Length == k.Length || !(char.IsLetterOrDigit(sub[k.Length]) || sub[k.Length] == '_');
                        }).Key;

                        if (key != null)
                        {
                            string value = Definitions[key];
                            line = line.Substring(0, i) + value + line.Substring(i + key.Length);
                            i += value.Length;
                        }
                    }
                }
            }

            return line;
        }
         
        private int FindComment(string line)
        {
            if (line.Length == 0) return -1;

            bool inString = (line[0] == '"');

            for (int i = 1; i < line.Length; i++)
            {
                if (line[i] == '"') inString = !inString;
                if (!inString && line[i] == '/' && line[i - 1] == '/')
                    return i - 1;
            }

            return -1;
        }

        private bool IsValidDefinition(string str)
        {
            return str.Length > 0 && str.All(c => char.IsLetterOrDigit(c) || c == '_') && !char.IsDigit(str[0]);
        }
    }
}
