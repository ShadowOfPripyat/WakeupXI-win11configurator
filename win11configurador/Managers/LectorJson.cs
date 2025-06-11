using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

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
                return JsonConvert.DeserializeObject<List<T>>(json);
            }
            catch (FileNotFoundException ex)
            {
                // Archivo no encontrado
                // Puedes registrar el error o devolver una lista vacía
                Console.WriteLine($"Archivo no encontrado: {ex.Message}");
                return new List<T>();
            }
            catch (UnauthorizedAccessException ex)
            {
                // Acceso denegado
                 Console.WriteLine($"Acces denegat: {ex.Message}");
                return new List<T>();
            }
            catch (JsonException ex)
            {
                // Error de deserialización JSON
                Console.WriteLine($"Error de JSON: {ex.Message}");
                return new List<T>();
            }
            catch (IOException ex)
            {
                // Otros errores de IO
                Console.WriteLine($"Error de IO: {ex.Message}");
                return new List<T>();
            }
            catch (Exception ex)
            {
                // Cualquier otra excepción
                Console.WriteLine($"Error inesperat: {ex.Message}");
                return new List<T>();
            }
        }

    }

}
