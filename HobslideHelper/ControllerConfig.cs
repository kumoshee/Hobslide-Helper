using Newtonsoft.Json;
using System.IO;

namespace HobslideHelper
{
    public class ControllerConfig
    {
        public InputBackend R1Backend { get; set; }
        public string R1DeviceGuid { get; set; }
        public int R1Button { get; set; }

        public InputBackend SquareBackend { get; set; }
        public string SquareDeviceGuid { get; set; }
        public int SquareButton { get; set; }

        public InputBackend CrossBackend { get; set; }
        public string CrossDeviceGuid { get; set; }
        public int CrossButton { get; set; }

        public static string ConfigPath =>
            "controller_config.json";

        public static ControllerConfig Load()
        {
            try
            {
                if (!File.Exists(ConfigPath))
                {
                    return CreateDefault();
                }

                string json =
                    File.ReadAllText(ConfigPath);

                return JsonConvert.DeserializeObject<ControllerConfig>(json);
            }
            catch
            {
                return CreateDefault();
            }
        }

        public void Save()
        {
            string json =
                JsonConvert.SerializeObject(
                    this,
                    Formatting.Indented);

            File.WriteAllText(ConfigPath, json);
        }

        static ControllerConfig CreateDefault()
        {
            return new ControllerConfig
            {
                R1Backend = InputBackend.DirectInput,
                R1DeviceGuid = "",
                R1Button = 5,

                SquareBackend = InputBackend.DirectInput,
                SquareDeviceGuid = "",
                SquareButton = 0,

                CrossBackend = InputBackend.DirectInput,
                CrossDeviceGuid = "",
                CrossButton = 1
            };
        }
    }
}