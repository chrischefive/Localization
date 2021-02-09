using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Chrische.Localization
{
    public static class EnumGenerator
    {
        public static bool Generate(TextDataBase dataBase)
        {
            string path = Application.dataPath + "/TextId.cs";
            using (StreamWriter sw = File.CreateText(path))
            {
                var content = string.Empty;
                content += "namespace Chrische.Localization\n";
                content += "{\n";
                content += "\tpublic enum TextId\n\t{\n";
                if (dataBase != null)
                {
                    foreach (var entry in dataBase.Entries)
                    {
                        content += "\t\t" + entry.ShadowId + ",\n";
                    }
                }
                
                content += "\t}\n";
                content += "}";
                sw.WriteLine(content);
            }
            return true;
        }
    }
}

