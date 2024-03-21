using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;

namespace PlaceableRestrict
{
    public class PRJsonSave
    {
        private static string pathprplayerdate = Rocket.Core.Environment.PluginsDirectory + "/PlaceableRestrict/BShopJsonPRPlayerDateSave.json";

        public static void Load()
        {
            if (!File.Exists(pathprplayerdate))
            {
                File.Create(pathprplayerdate).Close();
            }
            Dictionary<ulong, PRPlayerDate> dictionarybox = readJson<Dictionary<ulong, PRPlayerDate>>(pathprplayerdate);
            PRMain.Instance.PRPlayer = dictionarybox ?? new Dictionary<ulong, PRPlayerDate>();
        }
        public static void Save(Dictionary<ulong, PRPlayerDate> x)
        {
            writeJson(pathprplayerdate, x);
        }
        private static T readJson<T>(string _path)
        {
            return JsonConvert.DeserializeObject<T>(File.ReadAllText(_path));
        }

        private static void writeJson(string _path, object value)
        {
            File.WriteAllText(_path, JsonConvert.SerializeObject(value, Formatting.Indented));
        }
    }
}
