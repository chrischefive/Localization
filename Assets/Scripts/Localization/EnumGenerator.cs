using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Chrische.Localization
{
    public static class EnumGenerator
    {
        public static bool Generate(List<string> allValue)
        {
            string path = Application.dataPath + "/TextId.cs";
            using (StreamWriter sw = File.CreateText(path))
            {
                var content = string.Empty;
                content += "namespace Chrische.Localization\n";
                content += "{\n";
                content += "\tpublic enum TextId\n\t{\n";
                if (allValue.Count != 0)
                {
                    foreach (var value in allValue)
                    {
                        content += "\t\t" + value + ",\n";
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

