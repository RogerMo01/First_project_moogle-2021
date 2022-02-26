using System.IO;
using System.Collections.Generic;
using System.Text;

namespace MoogleEngine;

public class CorpusDB
{
    private static CorpusDB db = new CorpusDB();

    public string ContentRoot { get; private set; }
    public string[] DocumentsCollection { get; private set; }
    public Dictionary<string, Dictionary<string, int>> TF { get; private set; }
    public Dictionary<string, List<string>> Texts { get; set; }
    public Dictionary<string, string> Names { get; private set; }
    public List<string> Words { get; private set; }
    public int Size { get; private set; }

    private CorpusDB()
    {
        char separator = Path.DirectorySeparatorChar;
        
        // root de la carpeta Content
        this.ContentRoot = StringHandler.ReplaceLastDirectoryName(Directory.GetCurrentDirectory(), "Content");

        // Guarda todas las "roots" de los documentos del corpus
        this.DocumentsCollection = Directory.EnumerateFiles(this.ContentRoot).ToArray();

        // Guarda la cantidad de documentos
        this.Size = DocumentsCollection.Length;

        // Guarda los nombres de los documentos
        this.Names = new Dictionary<string, string>();
        foreach (var doc in DocumentsCollection)
        {
            Names.Add(doc, StringHandler.GetFileName(doc));
        }

        this.TF = new Dictionary<string, Dictionary<string, int>>();
        this.Texts = new Dictionary<string, List<string>>();
        this.Words = new List<string>();

        #region Read Document
        foreach (var doc in DocumentsCollection)
        {
            TF.Add(doc, new Dictionary<string, int>());

            // guarda todo el texto de un documento en un string
            string text = new StreamReader(doc).ReadToEnd();

            // separar el string por palabras
            List<string> words = new List<string>();
            string[] firstSeparation = text.Split("\r\n"); // separa los espaciados de bloques            

            foreach (var item in firstSeparation)
            {
                string[] intoSeparation = item.Split(" "); // separa los espacios de palabras
                foreach (var intoItem in intoSeparation)
                {
                    words.Add(intoItem);
                }
            }

            // por cada palabra del texto, hacerle refactor
            for (int i = 0; i < words.Count; i++)
            {
                words[i] = StringHandler.WordRefactor(words[i]);
            }

            words.RemoveAll(x => x == ""); // eliminar strings vacíos
            
            this.Texts.Add(doc, words);

            // por cada palabra del texto, contarla para llenar el TF
            for (int i = 0; i < words.Count; i++)
            {
                // si no está, añadirla a la Lista de palabras globales
                if (!this.Words.Contains(words[i])) Words.Add(words[i]);

                if (TF[doc].ContainsKey(words[i]))
                {
                    TF[doc][words[i]]++;
                }
                else
                {
                    TF[doc].Add(words[i], 1);
                }
            }
        }
        # endregion

    }

    public static CorpusDB GetDB { get{ return db; } }
}