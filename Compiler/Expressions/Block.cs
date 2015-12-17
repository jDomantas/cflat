using System.Collections.Generic;
using System.Linq;

namespace Compiler.Expressions
{
    class Block
    {
        public bool IsTerminating { get; }
        public Sentence[] Sentences { get; }

        public Block(Sentence[] sentences)
        {
            Sentences = sentences;

            IsTerminating = Sentences.Any(sentence => sentence.IsTerminating);
        }
        
        public static Block TryRead(SymbolStream stream)
        {
            var state = stream.SaveState();

            if (stream.Peek() == '{')
            {
                stream.Consume();
                List<Sentence> sentences = new List<Sentence>();

                stream.SkipWhitespace();
                while (stream.Peek() != '}')
                {
                    Sentence sentence = Sentence.TryRead(stream);
                    if (sentence == null)
                        return null;

                    sentences.Add(sentence);
                    stream.SkipWhitespace();
                }

                if (!stream.ExpectAndConsume('}', state))
                    return null;

                // remove definitions added in block
                state.RestoreDefinitions();

                return new Block(sentences.ToArray());
            }
            else if (stream.Peek() == ';')
            {
                // empty block
                stream.Consume();
                return new Block(new Sentence[0]);
            }
            else
            {
                Sentence sentence = Sentence.TryRead(stream);
                if (sentence == null)
                    return null;

                // remove definitions added in block
                state.RestoreDefinitions();

                return new Block(new Sentence[1] { sentence });
            }
        }

        public void Compile(CodeWriter writer, Definitions definitions)
        {
            definitions.EnterBlock();
            writer.IncreaseIdentationLevel();

            for (int i = 0; i < Sentences.Length; i++)
            {
                Sentences[i].Compile(writer, definitions);
                if (Sentences[i].IsTerminating)
                    break;
            }

            if (!IsTerminating)
            {
                int bytesToPop = definitions.ExitBlock();
                if (bytesToPop > 0)
                {
                    writer.WriteLine($"; exiting block, pop {bytesToPop} bytes");
                    writer.WriteLine($"add sp, {bytesToPop}");
                }
                else
                {
                    writer.WriteLine("; exiting block, nothing popped");
                }
            }
            else
            {
                writer.WriteLine("; block is terminating don't have to do anything here");
            }

            writer.DecreaseIdentationLevel();
        }
    }
}
