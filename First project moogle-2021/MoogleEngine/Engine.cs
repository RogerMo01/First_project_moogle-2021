namespace MoogleEngine;

public static class Engine
{
    
    public static string[] GetStopOrNeededDocuments(InputQuery inputQuery, List<string> clasificatedList)
    {
        CorpusDB dB = CorpusDB.GetDB;
        List<string> returnDocs = new List<string>();

        foreach (string word in clasificatedList)
        {
            // indice que tiene la palabra en todas las del query
            int indexInAllWords = IndexOf(inputQuery.allWords, word);

            // por cada documento, si contiene a esa palabra, entonces añadir a stopDocs
            foreach (var item in dB.TF)
            {
                if(item.Value.ContainsKey(word)) returnDocs.Add(item.Key);
            }
        }

        return returnDocs.ToArray();        
    }

    public static Dictionary<string, float> GetDocsScores(float[,] tf_Idf)
    {
        CorpusDB dB = CorpusDB.GetDB;
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
    
    
    #region Return Documents

        public static Dictionary<string, float> GetDocsToReturn(Dictionary<string, float> scores, string[] stopDocuments, string[] neededDocuments)
        {
            CorpusDB dB = CorpusDB.GetDB;
            
            // elimina de la lista los stopDocuments
            foreach (var doc in stopDocuments)
            { scores.Remove(doc); }

            // elimina de la lista los documentos donde no aparecen las neededWords
            if(neededDocuments.Length > 0)
            {
                string[] neededDocumentsComplement = GetDocsComplement(dB.DocumentsCollection, neededDocuments);
                foreach (var doc in neededDocumentsComplement)
                { scores.Remove(doc); }
            }                

            Dictionary<string, float> returnDocs = new Dictionary<string, float>();
            // ordena el Diccionario
            List<(string key, float value)> list = ToList(scores);        
            list = SortTill(list, 0.1f); // ordena los scores hasta un límite de score, el resto los elimina

            // decide cuántos documentos devolver (max 10)
            int returnSize = (list.Count < 10) ? list.Count : 10;

            for (int i = 0; i < returnSize; i++)
            {
                returnDocs.Add(list[i].key, list[i].value);
            }

            return returnDocs;        
        }

        private static List<(string, float)> ToList(Dictionary<string, float> dict)
        {
            List<(string, float)> list = new List<(string, float)>();
            
            foreach (var item in dict)
            {
                list.Add((item.Key, item.Value));
            }

            return list;
        }

        private static List<(string, float)> SortTill(List<(string, float)> list, float minScore)
        {
            List<(string, float)> newList = new List<(string, float)>();
            int size = list.Count();

            // encuentra el mayor elemento por cada iteración
            for (int i = 0; i < size; i++)
            {
                int index = GetMaxIndex(list);

                // no devolver documentos con score menor que el minScore
                if(list[index].Item2 <= minScore)
                {
                    list.RemoveAt(index);
                    continue;
                }

                newList.Add(list[index]);
                list.RemoveAt(index);
            }
            
            return newList;
        }

        private static int GetMaxIndex(List<(string doc, float score)> list)
        {
            int maxIndex = 0;

            for (int i = 1; i < list.Count; i++)
            {
                maxIndex = (list[i].score > list[maxIndex].score) ? i : maxIndex;
            }

            return maxIndex;
        }

        private static Dictionary<string, float> ToDictionary(List<(string key, float value)> list)
        {
            Dictionary<string, float> sortedScores = new Dictionary<string, float>();

            foreach (var item in list)
            {
                sortedScores.Add(item.key, item.value);
            }

            return sortedScores;
        }

        private static string[] GetDocsComplement(string[] allDocuments, string[] set)
        {
            List<string> complement = new List<string>();

            foreach (var doc in allDocuments)
            {
                if(!set.Contains(doc)) complement.Add(doc);
            }

            return complement.ToArray();
        }

    #endregion

    #region Snippets

        public static Dictionary<string, string> GetSnippets(string[] roots, InputQuery query, float[,] tfIdf)
        {
            CorpusDB dB = CorpusDB.GetDB;

            Dictionary<string, string> snippets = new Dictionary<string, string>();
            string[] words = query.allWords;

            // para saber cuales son las palabras más importantes del query
            List<(string word, float tfIdf)> wordScore = new List<(string word, float tfIdf)>();
            for (int i = 0; i < words.Length; i++)
            {
                wordScore.Add((words[i], SumInThisRow(tfIdf, i)));
            }

            // ordena por los scores
            wordScore = SortTill(wordScore, 0);

            // por cada documento guardar el snippet de la palabra más importante que él contenga
            for (int i = 0; i < roots.Length; i++)
            {
                for (int j = 0; j < wordScore.Count; j++)
                {
                    // si la palabra está en el documento
                    if (tfIdf[IndexOf(words, wordScore[j].word), IndexOf(dB.DocumentsCollection, roots[i])] > 0)
                    {
                        snippets.Add(roots[i], GetSnippetWith(wordScore[j].word, roots[i]));
                        break;
                    }
                }
            }
            
            return snippets;
        }
        private static float SumInThisRow(float[,] matrix, int row)
        {
            float sum = 0;

            for (int i = 0; i < matrix.GetLength(1); i++)
            {
                sum += matrix[row, i];
            }

            return sum;
        }
        public static int IndexOf(string[] collection, string element)
        {
            for (int i = 0; i < collection.Length; i++)
            {
                if(collection[i] == element) return i;
            }

            return -1;
        }
        private static string GetSnippetWith(string word, string root)
        {
            // copia todo el texto del documento
            string text = new StreamReader(root).ReadToEnd();

            List<string> sentences = new List<string>();

            // split por párrafos y unión con espacios
            string[] splited = text.Split("\r\n");
            text = String.Join(" ", splited);

            // split por oraciones
            splited = text.Split(".");
            foreach (var item2 in splited)
            {
                sentences.Add(item2);
            }

            // identifica en qué oración está la palabra y devolverla como snippet
            foreach (var sentc in sentences)
            {
                string[] separatedWords = sentc.Split(" ");

                for (int i = 0; i < separatedWords.Length; i++)
                {
                    if(StringHandler.WordRefactor(separatedWords[i]) == word)
                    {
                        // si la oración tiene más de 30 palabras, nos quedamos con la vecindad de la palabra clave de radio 15
                        if(separatedWords.Length > 30)
                        {
                            return CutSnippet(i, separatedWords, 15);
                        }
                        else
                        {
                            return sentc;
                        }

                    }
                    
                }
            }

            // éste caso solo debería ocurrir si la palabra es de ínfima importancia para el documento
            return "No hay snippet recomendado 🙃";
        }
        private static string CutSnippet(int index, string[] splitedSentence, int ratio)
        {
            string snippet = "";

            int startIndex = (index - 15 < 0) ? 0 : index - 15;
            int finalIndex = (index + 15 > splitedSentence.Length - 1) ? splitedSentence.Length - 1 : index + 15;

            for (int i = startIndex; i < finalIndex; i++)
            {
                snippet += splitedSentence[i] + " ";
            }

            return snippet;
        }


    #endregion

    #region Suggestion

        public static string MakeSuggestion(InputQuery inputQuery, float[,] tfIdf)
        {
            CorpusDB dB = CorpusDB.GetDB;
            string[] queryWords = inputQuery.allWords;
            string[] suggestion = new string[queryWords.Length];

            for (int i = 0; i < queryWords.Length; i++)
            {
                // comprueba si la ocurrencia de la palabra en todos los documentos es nula
                //o si en los documentos donde aparece es insignificante
                if(IsRowCleanOrIrrelevant(tfIdf, i))
                {
                    suggestion[i] = GetSuggestion(queryWords[i], dB.Words);
                }
                else
                {
                    suggestion[i] = queryWords[i];
                }
            }

            return String.Join(" ", suggestion);
        }

        private static bool IsRowCleanOrIrrelevant(float[,] tfIdf, int row)
        {
            for (int i = 0; i < tfIdf.GetLength(1); i++)
            {
                if(tfIdf[row, i] > 0.1f) return false;
            }

            return true;
        }

        private static string GetSuggestion(string word, List<string> wordsCollection)
        {
            List<string> suggestionList = new List<string>();
            // inicializar con valores despreciables
            double scoreMin = 1;
            
            // analizar la distancia de Levenshtein de la palabra con las del corpus
            foreach (var word2 in wordsCollection)
            {
                // analizar solo las palabras con un máximo de 2 carácteres de tamaño de diferencia
                if(Math.Abs(word.Length - word2.Length) <= 2)
                {
                    double tempScore = LevenshteinDistance(word, word2);

                    if(tempScore < scoreMin)
                    {
                        scoreMin = tempScore;
                        suggestionList.Clear();
                        suggestionList.Add(word2);
                    }
                    if(tempScore == scoreMin)
                    {
                        suggestionList.Add(word2);
                    }
                }
            }

            // analizar para quedarme con la mejor palabra para la sugerencia (mayor TF-IDF)
            CorpusDB dB = CorpusDB.GetDB;

            // calcula el TF-IDF de las posibles palabras de sugerencia
            float[,] tfIdf = Scorer.GetTf_Idf(suggestionList.ToArray());

            // tomar como score de la palabra la suma de todos sus TF-IDF en todos los documentos
            float[] wordScores = new float[suggestionList.Count];            
            for (int i = 0; i < wordScores.Length; i++)
            {
                wordScores[i] = SumInThisRow(tfIdf, i);
            }

            // inicializar con valores arbitrarios
            string suggWord = "*"; // en caso de no haber sugerencia se retorna ésta para luego identificar que no hubo sugerencia
            float suggScore = -1;
            
            // quedarme con el mayor 
            for (int i = 0; i < suggestionList.Count; i++)
            {
                if(wordScores[i] > suggScore && wordScores[i] > 0.1f)
                {
                    suggScore = wordScores[i];
                    suggWord = suggestionList[i];
                }
            }

            return suggWord;
        }

        public static double LevenshteinDistance(string wordRow, string wordCol)
        {
            int rows = wordRow.Length;
            int cols = wordCol.Length;

            // distancia mínima si uno de los string está vacío
            if(rows == 0) return cols;
            if(cols == 0) return rows;

            // crear la matriz de distancias
            int[,] distances = new int[rows + 1, cols + 1];

            // llenar la primera fila y columna con los índices
            for (int i = 0; i < cols; i++)
            { distances[0, i] = i; }
            for (int i = 1; i < rows; i++)
            { distances[i, 0] = i; }

            // recorre y llena la matriz de distancias
            for (int i = 1; i <= rows; i++)
            {
                for (int j = 1; j <= cols; j++)
                {
                    int startValue = (wordRow[i - 1] == wordCol[j - 1]) ? 0 : 1;
                    distances[i, j] = Min(distances[i-1, j] + 1, distances[i, j-1] + 1, distances[i-1, j-1] + startValue);
                }
            }

            int value = distances[rows, cols];

            // calcula el score final de distancia y lo devuelve
            if(rows > cols) 
            { return ((double)value / (double)rows); }
            else 
            { return ((double)value / (double)cols); }

        }
        private static int Min(int a, int b, int c)
        { return Math.Min(a, Math.Min(b, c)); }


    #endregion






};