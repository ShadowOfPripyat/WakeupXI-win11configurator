using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using win11configurador.Installers;
using win11configurador.plantillesjson;

namespace win11configurador.Managers
{
    public class LectorJson
    {
        // Esta clase se encarga de leer archivos JSON y devolver listas de objetos deserializados.
        // Se utiliza para cargar configuraciones, programas y otros datos necesarios para la aplicación

        public static List<T> LoadJson<T>(string filename)
        {
            string path = Path.Combine(".\\Dades\\", filename);
            try
            {
                string json = File.ReadAllText(path);
                var settings = new JsonSerializerSettings
                {
                    ContractResolver = new Newtonsoft.Json.Serialization.DefaultContractResolver
                    {
                        NamingStrategy = new Newtonsoft.Json.Serialization.SnakeCaseNamingStrategy()
                    }
                };
                return JsonConvert.DeserializeObject<List<T>>(json, settings);
            }
            catch (FileNotFoundException ex)
            {
                Console.WriteLine($"Archivo no encontrado: {ex.Message}");
                return new List<T>();
            }
            catch (UnauthorizedAccessException ex)
            {
                Console.WriteLine($"Acces denegat: {ex.Message}");
                return new List<T>();
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"Error de JSON: {ex.Message}");
                return new List<T>();
            }
            catch (IOException ex)
            {
                Console.WriteLine($"Error de IO: {ex.Message}");
                return new List<T>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error inesperat: {ex.Message}");
                return new List<T>();
            }
        }
        public static void SaveJson<T>(string filename, List<T> items)
        {
            string path = Path.Combine(".\\Dades\\", filename);
            try
            {
                var settings = new JsonSerializerSettings
                {
                    ContractResolver = new Newtonsoft.Json.Serialization.DefaultContractResolver
                    {
                        NamingStrategy = new Newtonsoft.Json.Serialization.SnakeCaseNamingStrategy()
                    },
                    Formatting = Formatting.Indented
                };
                string json = JsonConvert.SerializeObject(items, settings);
                File.WriteAllText(path, json);
            }
            catch (UnauthorizedAccessException ex)
            {
                Console.WriteLine($"Acceso denegado: {ex.Message}");
            }
            catch (IOException ex)
            {
                Console.WriteLine($"Error de IO: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error inesperado: {ex.Message}");
            }
        }

        public static Dictionary<string, List<WingetProgram>> LoadWingetJsonWithGroups(string filename)
        {
            string path = Path.Combine(".\\Dades\\", filename);
            try
            {
                string json = File.ReadAllText(path);
                var settings = new JsonSerializerSettings
                {
                    ContractResolver = new Newtonsoft.Json.Serialization.DefaultContractResolver
                    {
                        NamingStrategy = new Newtonsoft.Json.Serialization.SnakeCaseNamingStrategy()
                    }
                };
                // Deserializa el objeto raíz
                var root = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, List<WingetProgram>>>>(json, settings);
                // Extrae el grupo "All_programs"
                if (root != null && root.TryGetValue("All_programs", out var allPrograms))
                    return allPrograms;
                return new Dictionary<string, List<WingetProgram>>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error cargando JSON: {ex.Message}");
                return new Dictionary<string, List<WingetProgram>>();
            }

        }
        public static Dictionary<string, List<ConfigurationItem>> LoadConfigurationsWithGroups(string filename)
        {
            string path = Path.Combine(".\\Dades\\", filename);
            try
            {
                string json = File.ReadAllText(path);
                var settings = new JsonSerializerSettings
                {
                    ContractResolver = new Newtonsoft.Json.Serialization.DefaultContractResolver
                    {
                        NamingStrategy = new Newtonsoft.Json.Serialization.SnakeCaseNamingStrategy()
                    }
                };
                // Deserializa el objeto raíz
                var root = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, List<ConfigurationItem>>>>(json, settings);
                // Extrae el grupo "All_configurations"
                if (root != null && root.TryGetValue("All_configurations", out var allConfigs))
                    return allConfigs;
                return new Dictionary<string, List<ConfigurationItem>>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error cargando JSON: {ex.Message}");
                return new Dictionary<string, List<ConfigurationItem>>();
            }
        }
        public static List<ManualProgram> LoadManualPrograms(string path)
        {
            var json = File.ReadAllText(path);
            var root = Newtonsoft.Json.Linq.JObject.Parse(json);
            var allPrograms = root["All Programs"];
            var result = new List<ManualProgram>();

            foreach (var category in allPrograms.Children<JProperty>())
            {
                string categoryName = category.Name;
                foreach (var prog in category.Value)
                {
                    var manualProg = prog.ToObject<ManualProgram>();
                    manualProg.Category = categoryName;
                    result.Add(manualProg);
                }
            }
            return result;
        }

    }

}
