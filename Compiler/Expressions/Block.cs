using System.Collections.Generic;

namespace Compiler.Expressions
{
    class Block
    {
        public Sentence[] Sentences { get; }

        public Block(Sentence[] sentences)
        {
            Sentences = sentences;
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
                if (Sentences[i].GetType() == typeof(ReturnStatement))
                    break;
            }

            int bytesToPop = definitions.ExitBlock();
            if (bytesToPop > 0)
            {
                writer.WriteLine($"; exiting block, pop {bytesToPop} bytes");
                writer.WriteLine($"pop_bytes {bytesToPop}");
            }
            else
            {
                writer.WriteLine("; exiting block, nothing popped");
            }

            writer.DecreaseIdentationLevel();
        }
    }
}
