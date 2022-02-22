using System;
using System.IO;
using System.Text;
namespace MoogleEngine;
class DocumentStream
{   public Dictionary<string, int> TermFrequencies { get; private set; }
    public string Root { get; private set; }
    public string Name { get; private set; }
    public int Lines { get; private set; }
    public string Snippet { get; private set; }


    public DocumentStream(string root, InputQuery query)
    {
        this.Root = root;
        this.Name = GetNamefromRoot(root);
        this.TermFrequencies = FillDictionaryWords(query);
        
        int totalLines = 0;
        List<string> snippetsColection = new List<string>(); // guardaré cada linea donde aparezca una palabra

        // recorre todo el documento
        StreamReader docReader = new StreamReader(root);
        while (true)
        {
            string line = docReader.ReadLine();
            if (line == null) break;
            
            // analiza cada línea
            CheckLine(TermFrequencies, line, snippetsColection);
            totalLines++;
        }

        this.Lines = totalLines;

        // del conjundo de lineas donde aparecen las palabras, debo valorar con cual quedarme para el Snippet
        this.Snippet = GetSnippet(snippetsColection);

        

    }

    private static void CheckLine(Dictionary<string, int> tf, string line, List<string> snippets)
    {
        string[] lineWords = line.Split(" ");

        // si la palabra está en la búsqueda: la voy contando en el Diccionario y añado el snippet
        foreach (var item in lineWords)
        {
            if (tf.ContainsKey(item))
            {
                tf[item]++;
                snippets.Add(line);
            }
        }
    }

    private static string GetSnippet(List<string> snippets)
    {
        return "";
    }

    private static string GetNamefromRoot(string root)
    {
        // contar tamaño del substring que forma la extensión del archivo (.txt = 4)
        int extLength = 1;

        for (int i = root.Length - 1; i >= 0; i--)
        {
            if (root[i] == '.') break;
            extLength++;
        }

        // copia el nombre de la carpeta en reversa
        string revertedName = "";

        for (int i = root.Length - 1 - extLength; i >= 0; i--)
        {
            if(root[i] == '\\') break;
            revertedName += root[i];
        }

        // invierte el string
        string name = "";

        for (int i = revertedName.Length - 1; i >= 0; i--)
        {
            name += revertedName[i];
        }

        return name;
    }

    // crea un Diccionario con todos los términos
    private static Dictionary<string, int> FillDictionaryWords(InputQuery query)
    {
        Dictionary<string, int> tf = new Dictionary<string, int>();
        
        FillDictionaryList(tf, query.neededWords);
        FillDictionaryList(tf, query.ordinaryWords);
        FillDictionaryList(tf, query.stopWords);
        FillDictionaryList(tf, query.relatedWords);
        FillDictionaryList(tf, query.relevantWords);

        return tf;
    }

    // Llenan el Diccionario con los valores de las listas como las key
    private static void FillDictionaryList(Dictionary<string, int> dictionary, List<string> list)
    {
        foreach (var item in list)
        {
            dictionary.TryAdd(item, 0);
        }
    }
    private static void FillDictionaryList(Dictionary<string, int> dictionary, List<(string, string)> list)
    {
        foreach (var item in list)
        {
            dictionary.TryAdd(item.Item1, 0);
            dictionary.TryAdd(item.Item2, 0);
        }
    }
    private static void FillDictionaryList(Dictionary<string, int> dictionary, List<(int, string)> list)
    {
        foreach (var item in list)
        {
            dictionary.TryAdd(item.Item2, 0);
        }
    }



}