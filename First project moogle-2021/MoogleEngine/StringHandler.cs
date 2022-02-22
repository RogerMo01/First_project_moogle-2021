using System.Text;

namespace MoogleEngine;

public static class StringHandler
{

    public static string WordRefactor(string word)
    {
        foreach (var item in word)
        {
            if (!Char.IsLetter(item) && !Char.IsNumber(item))
            {
                word = word.Replace(item.ToString(), "");
            }
        }

        return word.ToLower();
    }

    #region Roots
    
    public static string GetFileName(string root)
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

    public static string ReplaceLastDirectoryName(string root, string newName)
    {
        root = $"{RemoveLastDirectoryName(root)}{newName}";        
        return root;
    }

    public static string RemoveLastDirectoryName(string root)
    {
        int counter = 0;

        for (int i = root.Length - 1; i >= 0; i--)
        {
            if (root[i] != '\\') counter++; else break;
        }

        root = root.Remove(root.Length - counter, counter);
        return root;
    }
    #endregion
}