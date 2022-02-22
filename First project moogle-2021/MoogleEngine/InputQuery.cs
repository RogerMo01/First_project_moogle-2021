namespace MoogleEngine;

public class InputQuery
{
    public List<string> ordinaryWords { get; private set; }
    public List<string> stopWords { get; private set; }
    public List<string> neededWords { get; private set; } 
    public List<(int stars, string word)> relevantWords { get; private set; }
    public List<(string word1, string word2)> relatedWords { get; private set; }
    public string[] allWords { get; private set; }

    public InputQuery(string query)
    {
        // separar el string del query en términos y ponerlos en una Lista
        List<string> terms = query.Split(" ").ToList();

        // si el query tiene más de un espacio consecutivo, el Split() devuelve un ("") de por medio, así los eliminamos
        terms.RemoveAll(x => x == ""); 
        
        // primeramente analizar si hay palabras relacionadas por el operador (~), y eliminarlas de la lista
        this.relatedWords = RelatingOperatorAnalyzer(ref terms);        
        // luego analizamos la existencia del resto de los operadores
        this.stopWords = new List<string>();
        this.neededWords = new List<string>();
        this.relevantWords = new List<(int index, string word)>();
        this.ordinaryWords = new List<string>();

        foreach (string term in terms)
        {
            switch (term[0])
            {
                case '!':
                stopWords.Add(StringHandler.WordRefactor(term));
                break;

                case '^':
                neededWords.Add(StringHandler.WordRefactor(term));
                break;

                case '*':
                relevantWords.Add(GetRelevantWordTuple(term));
                break;

                default:
                ordinaryWords.Add(StringHandler.WordRefactor(term));
                break;
            }
        }

        this.allWords = GetAllWords(terms);
    }

    
    private static (int stars, string word) GetRelevantWordTuple(string term)
    {
        int counter = 1;

        for (int i = 1; i < term.Length; i++)
        {
            if (term[i] != '*') break;
            else counter++;
        }

        return (counter, StringHandler.WordRefactor(term));
    }

    // solo funciona con par de términos y los pone en una lista
    private static List<(string , string)> RelatingOperatorAnalyzer(ref List<string> terms)
    {
        // si el operador aparece de primero o de último, no tiene sentido su existencia
        if (terms.First() == "~" || terms.First().First() == '~') { terms.RemoveAt(0); }
        if (terms.Last() == "~" || terms.Last().Last() == '~') { terms.RemoveAt(terms.Count - 1); }
        
        List<(string , string)> returnList = new List<(string , string)>();

        // verificar si el operador está unido a dos palabras sin espacios entre ellas, entonces lo separo
        terms = RelatOperatorImplicitIndex(terms);

        if (!terms.Any(x => x == "~")) { return returnList; } // si no hay ocurrencias del elemento ("~") en la Lista, entonces no hay elementos relacionados
        
        terms.RemoveAll(x => x == "");

        // busca el operador (~)
        for (int i = 1; i < terms.Count - 1; i++)
        {
            if (terms[i] == "~")
            {
                // una vez encontrado el operador, añadir a la lista ambos términos que él relaciona
                returnList.Add((StringHandler.WordRefactor(terms[i-1]), StringHandler.WordRefactor(terms[i + 1])));
                terms.RemoveAt(i); // remover el operador de la Lista de términos generales
            }
        }

        return returnList;
    }


    private static List<string> RelatOperatorImplicitIndex(List<string> list)
    {
        List<string> newList = new List<string>();

        for (int i = 0; i < list.Count; i++)
        {
            // caso en que la palabra comience con el operador
            if(list[i].Last() == '~')
            {
                newList.Add(StringHandler.WordRefactor(list[i]));
                newList.Add("~");
                continue;
            }

            // caso en que la palabra termine con el operador
            if(list[i].First() == '~')
            {
                newList.Add("~");
                newList.Add(StringHandler.WordRefactor(list[i]));
                continue;
            }

            // caso en el que el operador esté implícito entre dos palabras
            string tempTerm = "";
            foreach (var item in list[i]) // va verificando cada char
            {
                if (item != '~')
                {
                    tempTerm += item;
                }
                else
                {
                    newList.Add(StringHandler.WordRefactor(tempTerm));
                    newList.Add("~");
                    tempTerm = "";
                }

            }

            newList.Add(tempTerm);
        }

        return newList;
    }

    private static string[] GetAllWords(List<string> list)
    {
        string[] words = new string[list.Count];

        for (int i = 0; i < words.Length; i++)
        {
            words[i] = StringHandler.WordRefactor(list[i]);
        }

        return words;
    }


}