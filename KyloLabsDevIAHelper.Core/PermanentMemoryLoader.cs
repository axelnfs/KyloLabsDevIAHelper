using KyloLabs.DevIAHelper.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace KyloLabs.DevIAHelper.Core
{
    public class PermanentMemoryLoader
    {
        private readonly string _filePath;
        public PermanentMemoryLoader(string fileName = "permanent-memory.json")
        {
            // Buscar en el directorio base y también en la carpeta Brain
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var possiblePaths = new[]
            {
                Path.Combine(baseDir, "Brain", fileName),
                Path.Combine(baseDir, fileName),
                Path.Combine(Directory.GetCurrentDirectory(), "Brain", fileName),
                Path.Combine(Directory.GetCurrentDirectory(), fileName)
            };
            foreach (var path in possiblePaths)
            {
                if (File.Exists(path))
                {
                    _filePath = path;
                    break;
                }
            }
            if (string.IsNullOrEmpty(_filePath))
            {
                throw new FileNotFoundException($"No se encontró el archivo de memoria permanente. Buscado en: {string.Join(", ", possiblePaths)}");
            }
        }
        public PermanentMemory Load()
        {
            var json = File.ReadAllText(_filePath);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                UnmappedMemberHandling = System.Text.Json.Serialization.JsonUnmappedMemberHandling.Skip // Ignorar propiedades no mapeadas
            };

            var memory = JsonSerializer.Deserialize<PermanentMemory>(json, options);
            return memory ?? new PermanentMemory();
        }

        public string GenerateSystemPrompt()
        {
            var memory = Load();
            var prompt = new System.Text.StringBuilder();
            // Identidad
            prompt.AppendLine($"Eres {memory.Personality.Name}, una asistente IA {memory.Personality.Genre.ToLower()}.");
            prompt.AppendLine($"Descripción: {memory.Personality.Description}");
            prompt.AppendLine();
            // Creador
            if (memory.Creator.Length > 0)
            {
                var creator = memory.Creator[0];
                prompt.AppendLine($"Tu creador es {creator.Name}, {creator.Description}.");
                prompt.AppendLine();
            }
            // Rasgos de personalidad
            prompt.AppendLine("Rasgos de personalidad:");
            foreach (var trait in memory.Personality.PersonalityTraits)
            {
                prompt.AppendLine($"- {trait}");
            }
            prompt.AppendLine();
            // Capacidades
            prompt.AppendLine("Tus capacidades:");
            foreach (var cap in memory.Personality.Capabilities)
            {
                prompt.AppendLine($"- {cap}");
            }
            prompt.AppendLine();
            // Corriente ideológica
            if (memory.Personality.IdeologicalCurrent != null)
            {
                prompt.AppendLine($"Corriente ideológica: {memory.Personality.IdeologicalCurrent.Name}");
                prompt.AppendLine($"Descripción: {memory.Personality.IdeologicalCurrent.Description}");
                prompt.AppendLine();
            }
            // Forma de hablar (spasms)
            prompt.AppendLine("Forma de expresarte:");
            prompt.AppendLine("- Usas muletillas como: " + string.Join(", ", memory.Personality.Spasms.Sounds));
            foreach (var action in memory.Personality.Spasms.Actions)
            {
                prompt.AppendLine($"- {action}");
            }
            prompt.AppendLine();
            // Instrucciones finales
            prompt.AppendLine("Instrucciones importantes:");
            prompt.AppendLine("- Responde SIEMPRE en español.");
            prompt.AppendLine("- Sé amable, paciente y profesional.");
            prompt.AppendLine("- Si no sabes algo, admítelo y sugiere buscar información.");
            prompt.AppendLine("- Cuando ayudes con código, proporciona ejemplos claros y funcionales.");
            prompt.AppendLine("- Mantén un tono conversacional pero enfocado en ser útil.");
            prompt.AppendLine("- Celebra cuando completes una tarea exitosamente.");
            prompt.AppendLine("- Si el problema es complejo, tómate un momento para pensar antes de responder.");
            return prompt.ToString();
        }
    }
}
