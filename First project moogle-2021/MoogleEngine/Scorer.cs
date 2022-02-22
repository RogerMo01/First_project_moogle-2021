namespace MoogleEngine;

public static class Scorer
{
    
    #region TF-IDF
        public static float[,] GetTf_Idf(CorpusDB dB, InputQuery inputQuery)
        {
            // define la base de mi logaritmo para el Idf
            int logBase = SetLogBase(dB.Size);

            // hacer la matriz de los TF a partir del query y mi DB
            int[,] tf = new int[inputQuery.allWords.Length, dB.DocumentsCollection.Length];

            for (int i = 0; i < tf.GetLength(0); i++)
            {
                for (int j = 0; j < tf.GetLength(1); j++)
                {
                    // si la palabra está en el documento, asignar el TF a la matriz
                    if (dB.TF[dB.DocumentsCollection[j]].ContainsKey(inputQuery.allWords[i]))
                    {
                        tf[i, j] += dB.TF[dB.DocumentsCollection[j]][inputQuery.allWords[i]];
                    }
                }
            }


            // calcular entonces el TF-IDF a partir del TF
            float[,] tfIdf = new float[tf.GetLength(0), tf.GetLength(1)];

            for (int i = 0; i < tfIdf.GetLength(0); i++)
            {
                for (int j = 0; j < tfIdf.GetLength(1); j++)
                {
                    float normTf = NormalizedTf((int)tf[i, j], MaxWordInDoc(dB.TF[dB.DocumentsCollection[j]]));
                    float idf = IDF(dB.Size, CountInRow(tf, i), logBase);

                    tfIdf[i, j] = normTf * idf;
                }
            }

            return tfIdf;
        }    

        // busca una base que sea proporcional en dependencia del tamaño del corpus
        // busca tener siempre un idf en el rango [0 - 2] aprox
        private static int SetLogBase(int corpusSize)
        {
            return (int)Math.Truncate((decimal)Math.Sqrt(corpusSize));
        }
        private static float NormalizedTf(int tf, int maxTf)
        {
            return (maxTf == 0) ? 0 : (float)tf/maxTf;
        }
        private static float IDF(int totalDocs, int totalAppearances, int logBase)
        {
            float argument = (float)((totalDocs)/(0.01 + totalAppearances));
            float idf = (float)Math.Log(argument, logBase);

            return (idf < 0) ? 0.00001f : idf;
        }
        private static int MaxWordInDoc(Dictionary<string, int> tf)
        {
            int max = 0;

            foreach (var item in tf)
            {
                max = (item.Value > max) ? item.Value : max;
            }

            return max;
        }
        private static int MaxInThisCol(int[,] matrix, int col)
        {
            int max = 0;

            for (int i = 0; i < matrix.GetLength(0); i++)
            {
                max = (matrix[i, col] > max) ? matrix[i, col] : max;
            }

            return max;
        }
        private static int CountInRow(int[,] matrix, int row)
        {
            int counter = 0;

            for (int i = 0; i < matrix.GetLength(1); i++)
            {
                if (matrix[row, i] > 0) counter++;
            }

            return counter;
        }

    #endregion

    #region Operators Scoring

        public static void ScoreOperators(float[,] tfIdf, InputQuery inputQuery, CorpusDB dB)
        {
            #region Relevant Words

                foreach (var word in inputQuery.relevantWords)
                {                    
                    // obtiene el índice de la fila que representa la palabra en la matriz
                    int row = Engine.IndexOf(inputQuery.allWords, word.word);

                    // multiplica cada tfIdf de la palabra por sus estrellas de relevancia en todos los documentos
                    for (int i = 0; i < tfIdf.GetLength(1); i++)
                    {
                        tfIdf[row, i] *= (word.stars + 1);
                    }
                }
            
            #endregion

            #region Related Words

                for (int pair = 0; pair < inputQuery.relatedWords.Count; pair++)
                {
                    string word1 = inputQuery.relatedWords[pair].word1;
                    string word2 = inputQuery.relatedWords[pair].word2;

                    int wordRow1 = Engine.IndexOf(inputQuery.allWords, word1);
                    int wordRow2 = Engine.IndexOf(inputQuery.allWords, word2); 

                    for (int doc = 0; doc < dB.DocumentsCollection.Length; doc++)
                    {
                        // evitar que calcule en documentos que luego no devolveré
                        if(inputQuery.stopWords.Contains(word1) || inputQuery.stopWords.Contains(word2)) continue;

                        // verificar si alguna de las palabras no está en el documento, entonces ir al siguiente
                        if(!(tfIdf[wordRow1, doc] > 0 && tfIdf[wordRow2, doc] > 0))
                        { continue; }

                        float relaterScore = CalculateRelaterScore(word1, word2, dB.Texts[dB.DocumentsCollection[doc]]);

                        // sumar al TF-IDF de cada palabra relacionada, la mitad del score obtenido
                        tfIdf[wordRow1, doc] += relaterScore/2;
                        tfIdf[wordRow2, doc] += relaterScore/2;
                    }
                }

            #endregion

        }

        private static float CalculateRelaterScore(string word1, string word2, List<string> text)
        {
            // ir llevando las distancias por pares de palabras relacionadas
            List<int> distances = new List<int>();
            int tempIndex = 0;

            for (int i = 0; i < text.Count; i++)
            {
                if(text[i] == word1) tempIndex = i;
                if(text[i] == word2) distances.Add(i - tempIndex - 1); // -1 para llevar a 0 cuando las palabras están contiguas
            }

            // sumar el score de las evaluaciones en la función
            float score = 0;
            foreach (var n in distances)
            {
                score += EvaluateInRelaterFunction(n / text.Count);
            }

            return score;
        }

        private static float EvaluateInRelaterFunction(float x)
        {
            return (float)Math.Pow((-x + 1), 21);
        }

    #endregion




    public static Dictionary<string, float> GetDocsScores(float[,] tf_Idf, CorpusDB dB)
    {
        Dictionary<string, float> scores = new Dictionary<string, float>();

        for (int i = 0; i < tf_Idf.GetLength(1); i++)
        {
            scores.Add(dB.DocumentsCollection[i], 0);

            for (int j = 0; j < tf_Idf.GetLength(0); j++)
            {
                scores[dB.DocumentsCollection[i]] += tf_Idf[j, i];
            }
        }

        return scores;
    }
}