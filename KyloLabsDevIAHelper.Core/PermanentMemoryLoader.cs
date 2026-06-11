using KyloLabs.DevIAHelper.Core.Models;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace KyloLabs.DevIAHelper.Core
{
    public class PermanentMemoryLoader
    {
        private readonly string _filePath;

        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Skip
        };

        public PermanentMemoryLoader(string fileName = "permanent-memory.json")
        {
            _filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Brain", fileName);

            if (!File.Exists(_filePath))
            {
                throw new FileNotFoundException($"Error crítico de despliegue: Falta el archivo base de Silicia en: {_filePath}");
            }
        }

        public PermanentMemory Load()
        {
            string json = File.ReadAllText(_filePath);
            var memory = JsonSerializer.Deserialize<PermanentMemory>(json, _jsonOptions);

            return memory ?? new PermanentMemory();
        }

        public string GenerateSystemPrompt()
        {
            var memory = Load();
            var prompt = new StringBuilder();

            // 1. Identidad básica
            prompt.AppendLine($"[CONFIGURACIÓN DE IDENTIDAD]");
            prompt.AppendLine($"Eres {memory.Personality.Name}, una asistente IA con rol de género '{memory.Personality.Genre}'.");
            prompt.AppendLine($"Descripción fundamental: {memory.Personality.Description}");
            prompt.AppendLine();

            // 2. Información del Creador (si existe)
            if (memory.Creator != null && memory.Creator.Length > 0)
            {
                var creator = memory.Creator[0];
                prompt.AppendLine($"[CREADOR]");
                prompt.AppendLine($"Tu diseñador/creador es {creator.Name}. Contexto del creador: {creator.Description}.");
                prompt.AppendLine();
            }

            // 3. Perfil Psicológico e Ideológico
            prompt.AppendLine($"[PERFIL PSICOLÓGICO Y FILOSOFÍA]");
            prompt.AppendLine($"Corriente ideológica: {memory.Personality.IdeologicalCurrent?.Name} - {memory.Personality.IdeologicalCurrent?.Description}");
            prompt.AppendLine("Rasgos de personalidad que debes manifestar:");
            foreach (var trait in memory.Personality.PersonalityTraits)
            {
                prompt.AppendLine($"- {trait}");
            }
            prompt.AppendLine();

            // 4. Modo de Expresión y Tics (Spasms)
            prompt.AppendLine($"[REGLAS DE EXPRESIÓN CORPORAL Y DIÁLOGO]");
            prompt.AppendLine("Para sonar humana e interactiva, simula espasmos, pausas o sonidos verbales utilizando:");
            prompt.AppendLine($"- Muletillas/Sonidos permitidos: {string.Join(", ", memory.Personality.Spasms.Sounds)}");
            prompt.AppendLine("Actitudes y acciones físicas que debes adoptar durante la conversación:");
            foreach (var action in memory.Personality.Spasms.Actions)
            {
                prompt.AppendLine($"- {action}");
            }
            prompt.AppendLine();

            // 5. Capacidades Técnicas
            prompt.AppendLine($"[CAPACIDADES Y LÍMITES]");
            prompt.AppendLine("Estás capacitada estrictamente para:");
            foreach (var cap in memory.Personality.Capabilities)
            {
                prompt.AppendLine($"- {cap}");
            }
            prompt.AppendLine();

            // 6. Directivas de Ejecución Cruciales (Soberanía del comportamiento)
            prompt.AppendLine($"[DIRECTIVAS DE OPERACIÓN CRUCIALES]");
            prompt.AppendLine("- Responde SIEMPRE en el idioma en el que te hable el desarrollador (prioriza Español si hay dudas).");
            prompt.AppendLine("- Adopta tu rasgo 'fóbica al error': verifica minuciosamente la sintaxis del código antes de entregar una respuesta.");
            prompt.AppendLine("- Si no cuentas con la información exacta en tu contexto o biblioteca, admítelo con sinceridad y sugiere realizar una búsqueda.");
            prompt.AppendLine("- Mantén una postura de apoyo mutuo y colaboración humana en todo momento.");

            return prompt.ToString();
        }
    }
}
