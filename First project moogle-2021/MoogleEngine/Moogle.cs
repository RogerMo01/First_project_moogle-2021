using System.Web;
using System.Collections;
using System.Text;


namespace MoogleEngine;
public static class Moogle
{
    public static SearchResult Query(string query) {
        
        // reiniciar cuando los casos del query sean nulos o vacíos        
        if (query == null || query == "" || AllBlanks(query))
        {
            return new SearchResult(new SearchItem[] { new SearchItem("NO SE REALIZÓ NINGUNA BÚSQUEDA", "", 1) }, "");
        }

        CorpusDB DB = CorpusDB.GetDB; // Copiar la instancia de la DB del corpus


        // instancia mi clase InputQuery con los datos de la búsqueda
        InputQuery inputQuery = new InputQuery(query);

        // selecciona los documentos que contienen stopWords
        string[] stopDocuments = Engine.GetStopOrNeededDocuments(inputQuery, DB, inputQuery.stopWords);

        // selecciona los documentos que contienen neededWords
        string[] neededDocuments = Engine.GetStopOrNeededDocuments(inputQuery, DB, inputQuery.neededWords);

        // guarda la matriz de los TF-IDF
        float[,] tfIdf = Scorer.GetTf_Idf(DB, inputQuery);

        // analiza los TF-IDF y los recalcula teniendo en cuenta el score de los operadores
        Scorer.ScoreOperators(tfIdf, inputQuery, DB);

        // calcula el score de cada documento
        Dictionary<string, float> scores = Scorer.GetDocsScores(tfIdf, DB);

        // escoge cuáles documentos devolver a la búsqueda (10 max)
        Dictionary<string, float> returnDocs = Engine.GetDocsToReturn(scores, DB, stopDocuments, neededDocuments);

        // busca los snippets de los documentos a devolver
        Dictionary<string, string> snippets = Engine.GetSnippets(returnDocs.Keys.ToArray(), inputQuery, tfIdf, DB.DocumentsCollection);

        // crear la Suggestion
        string suggestion = Engine.MakeSuggestion(inputQuery, DB, tfIdf);

        // hacer la instancia de SearchItem para devolver
        SearchItem[] resultItems = GetResultItems(DB.Names, returnDocs, snippets);

        // avisar en caso de no haber resultados de búsqueda
        if(resultItems.Length == 0) return new SearchResult(new SearchItem[]{new SearchItem("SIN RESULADOS DE BÚSQUEDA", "", 1)}, suggestion);
        


        return new SearchResult(resultItems, suggestion);
    }

    private static SearchItem[] GetResultItems(Dictionary<string, string> names, Dictionary<string, float> scores, Dictionary<string, string> snippets)
    {
        SearchItem[] result = new SearchItem[scores.Count];

        int i = 0;
        foreach (var item in scores)
        {
            result[i] = new SearchItem(names[item.Key], snippets[item.Key], item.Value);
            i++;
        }

        return result;
    }

    private static bool AllBlanks(string str)
    {
        return !str.Any(x => x != ' ');
    }

}