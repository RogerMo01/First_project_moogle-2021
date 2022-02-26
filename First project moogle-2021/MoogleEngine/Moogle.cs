using System.Web;
using System.Collections;
using System.Text;


namespace MoogleEngine;
public static class Moogle
{
    public static SearchResult Query(string query) {
        
        if (query == null || query == "" || AllBlanks(query))
        {
            return new SearchResult(new SearchItem[] { new SearchItem("NO SE REALIZÓ NINGUNA BÚSQUEDA", "", 1) }, "");
        }

        // instancia mi clase InputQuery con los datos de la búsqueda
        InputQuery inputQuery = new InputQuery(query);

        // selecciona los documentos que contienen stopWords
        string[] stopDocuments = Engine.GetStopOrNeededDocuments(inputQuery, inputQuery.stopWords);

        // selecciona los documentos que contienen neededWordspowe
        string[] neededDocuments = Engine.GetStopOrNeededDocuments(inputQuery, inputQuery.neededWords);

        // guarda la matriz de los TF-IDF
        float[,] tfIdf = Scorer.GetTf_Idf(inputQuery.allWords);

        // analiza los TF-IDF y los recalcula teniendo en cuenta el score de los operadores
        Scorer.ScoreOperators(tfIdf, inputQuery);

        // calcula el score de cada documento
        Dictionary<string, float> scores = Engine.GetDocsScores(tfIdf);

        // escoge cuáles documentos devolver a la búsqueda (10 max)
        Dictionary<string, float> returnDocs = Engine.GetDocsToReturn(scores, stopDocuments, neededDocuments);

        // busca los snippets de los documentos a devolver
        Dictionary<string, string> snippets = Engine.GetSnippets(returnDocs.Keys.ToArray(), inputQuery, tfIdf);

        // crear la sugerencia
        string suggestion = Engine.MakeSuggestion(inputQuery, tfIdf);

        // hacer la instancia de SearchItem para devolver
        SearchItem[] resultItems = GetResultItems(returnDocs, snippets);

        // avisar en caso de no haber resultados de búsqueda
        if(resultItems.Length == 0) return new SearchResult(new SearchItem[]{new SearchItem("SIN RESULADOS DE BÚSQUEDA", "", 1)}, suggestion);
        

        return new SearchResult(resultItems, suggestion);
    }

    private static SearchItem[] GetResultItems(Dictionary<string, float> scores, Dictionary<string, string> snippets)
    {
        CorpusDB dB = CorpusDB.GetDB;

        SearchItem[] result = new SearchItem[scores.Count];

        int i = 0;
        foreach (var item in scores)
        {
            result[i] = new SearchItem(dB.Names[item.Key], snippets[item.Key], item.Value);
            i++;
        }

        return result;
    }

    private static bool AllBlanks(string str)
    {
        return !str.Any(x => x != ' ');
    }

}