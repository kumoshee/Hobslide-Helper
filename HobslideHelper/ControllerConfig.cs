using Newtonsoft.Json;
using System.IO;

namespace HobslideHelper
{
    public class ControllerConfig
    {
        public int R1Button { get; set; }
        public int SquareButton { get; set; }
        public int CrossButton { get; set; }

        public static string ConfigPath => "controller_config.json";

        public static ControllerConfig Load()
        {
            try
            {
                if (!File.Exists(ConfigPath))
                {
                    return CreateDefault();
                }

                string json = File.ReadAllText(ConfigPath);

                return JsonConvert.DeserializeObject<ControllerConfig>(json);
            }
            catch
            {
                return CreateDefault();
            }
        }

        public void Save()
        {
            string json = JsonConvert.SerializeObject(this, Formatting.Indented);

            File.WriteAllText(ConfigPath, json);
        }

        static ControllerConfig CreateDefault()
        {
            return new ControllerConfig
            {
                R1Button = 5,
                SquareButton = 0,
                CrossButton = 1
            };
        }
    }
}