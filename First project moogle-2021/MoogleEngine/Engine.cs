namespace MoogleEngine;

public static class Engine
{
    
    public static string[] GetStopOrNeededDocuments(InputQuery inputQuery, CorpusDB dB, List<string> clasificatedList)
    {
        List<string> returnDocs = new List<string>();

        // itera por cada stopWord
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

    
    


    #region Return Documents

        public static Dictionary<string, float> GetDocsToReturn(Dictionary<string, float> scores, CorpusDB dB, string[] stopDocuments, string[] neededDocuments)
        {
            #region Operadores
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
            #endregion

            Dictionary<string, float> returnDocs = new Dictionary<string, float>();
            // ordena el Diccionario
            List<(string key, float value)> list = ToList(scores);        
            list = SortTill(list, 0.001f); // ordena los scores hasta un límite de score, el resto los elimina

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

        public static Dictionary<string, string> GetSnippets(string[] roots, InputQuery query, float[,] tfIdf, string[] documentsCollection)
        {
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
                    if (tfIdf[IndexOf(words, wordScore[j].word), IndexOf(documentsCollection, roots[i])] > 0)
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
                            return CropSnippet(i, separatedWords, 15);
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
        private static string CropSnippet(int index, string[] splitedSentence, int ratio)
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

        public static string MakeSuggestion(InputQuery inputQuery, CorpusDB dB, float[,] tfIdf)
        {
            string[] queryWords = inputQuery.allWords;
            string[] suggestion = new string[queryWords.Length];

            for (int i = 0; i < queryWords.Length; i++)
            {
                // comprueba si la ocurrencia de la palabra en todos los documentos es nula e ir llenando la suggestion
                if(IsRowClean(tfIdf, i))
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

        private static bool IsRowClean(float[,] tfIdf, int row)
        {
            for (int i = 0; i < tfIdf.GetLength(1); i++)
            {
                if(tfIdf[row, i] != 0) return false;
            }

            return true;
        }

        private static string GetSuggestion(string word, List<string> wordsCollection)
        {
            // inicializar con valores despreciables
            string suggested = word;
            double score = 1;
            
            // analizar la distancia de Levenshtein de la palabra con las del corpus
            foreach (var item in wordsCollection)
            {
                // analizar solo las palabras con un máximo de 2 carácteres de tamaño de diferencia
                if(Math.Abs(word.Length - item.Length) <= 2)
                {
                    double tempScore = LevenshteinDistance(word, item);

                    if(tempScore < score) // actualiza
                    {
                        score = tempScore;
                        suggested = item;
                    }
                }
            }

            return suggested;
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