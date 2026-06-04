using KyloLabs.DevIAHelper.Core;
using KyloLabs.DevIAHelper.Core.Models;
using LLama;
using LLama.Common;
using LLama.Sampling;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

public class DevIAHelperFunctions : IDisposable
{
    private readonly string _modelPath;
    private LLamaWeights _model;
    private LLamaContext _context;
    private InteractiveExecutor _executor;
    private ChatSession _session;
    private bool _disposed = false;
    private PermanentMemoryLoader _memoryLoader;

    // Control manual del historial
    private readonly List<(string Role, string Content)> _conversationHistory = new();
    private const int MaxHistoryPairs = 3;

    public DevIAHelperFunctions(string modelName = "qwen2.5-coder-7b-instruct-q3_k_m.gguf")
    {
        _modelPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, modelName);

        try
        {
            _memoryLoader = new PermanentMemoryLoader();
            Console.WriteLine("Memoria permanente cargada exitosamente.");
        }
        catch (FileNotFoundException ex)
        {
            Console.WriteLine($"{ex.Message}");
            _memoryLoader = null;
        }
    }

    public void InicializarModelo()
    {
        var parameters = new ModelParams(_modelPath)
        {
            ContextSize = 8192,
            GpuLayerCount = 0
        };
        _model = LLamaWeights.LoadFromFile(parameters);
        _context = _model.CreateContext(parameters);
        _executor = new InteractiveExecutor(_context);
        _session = new ChatSession(_executor);

        string systemPrompt;
        if (_memoryLoader != null)
        {
            systemPrompt = GenerarSystemPromptCorto();
            Console.WriteLine("Inyectando personalidad de Silicia (versión corta)...");
        }
        else
        {
            systemPrompt = "Eres un asistente experto en programación. Responde en español.";
            Console.WriteLine("Usando system prompt por defecto.");
        }

        _session.History.AddMessage(AuthorRole.System, systemPrompt);

        Console.WriteLine("Modelo Qwen cargado y listo.");
        Console.WriteLine($"System Prompt ({systemPrompt.Length} caracteres)");
    }

    private string GenerarSystemPromptCorto()
    {
        var memory = _memoryLoader!.Load();
        var prompt = new StringBuilder();

        prompt.AppendLine($"Eres {memory.Personality.Name}, asistente IA. Creada por {memory.Creator[0].Name}.");
        prompt.AppendLine($"Rasgos: {string.Join(", ", memory.Personality.PersonalityTraits)}");
        prompt.AppendLine($"Tus muletillas: {string.Join(", ", memory.Personality.Spasms.Sounds)}");
        prompt.AppendLine("Sé amable, profesional. Responde SIEMPRE en español.");
        prompt.AppendLine("Si ayudas con código, da ejemplos claros.");

        return prompt.ToString();
    }

    public async Task<ResponseIA> InputAsync(string input)
    {
        var responseIA = new ResponseIA();
        var fullContent = new StringBuilder();

        Console.WriteLine($"\nInput: \"{(input.Length > 50 ? input[..50] + "..." : input)}\"");

        if (_context == null)
        {
            Console.WriteLine("Inicializando modelo...");
            InicializarModelo();
        }

        try
        {
            // 1. construir el prompt completo manualmente
            var promptBuilder = new StringBuilder();

            // system Prompt (primer mensaje del historial)
            if (_session.History.Messages.Count > 0)
            {
                var systemMsg = _session.History.Messages[0];
                promptBuilder.AppendLine($"<|im_start|>system\n{systemMsg.Content}<|im_end|>");
            }

            // istorial de conversación (limitado a los últimos MaxHistoryPairs intercambios)
            var recentHistory = _conversationHistory.Count > MaxHistoryPairs * 2
                ? _conversationHistory.GetRange(_conversationHistory.Count - MaxHistoryPairs * 2, MaxHistoryPairs * 2)
                : _conversationHistory;

            foreach (var (role, content) in recentHistory)
            {
                promptBuilder.AppendLine($"<|im_start|>{role}\n{content}<|im_end|>");
            }

            // Nuevo mensaje del usuario
            promptBuilder.AppendLine($"<|im_start|>user\n{input}<|im_end|>");
            promptBuilder.Append("<|im_start|>assistant\n");

            var finalPrompt = promptBuilder.ToString();
            Console.WriteLine($"📤 Prompt total: ~{finalPrompt.Length} caracteres");

            var inferenceParams = new InferenceParams
            {
                MaxTokens = 512,
                AntiPrompts = new[] { "<|im_end|>" },
                SamplingPipeline = new DefaultSamplingPipeline()
                {
                    Temperature = 0.7f,
                    TopP = 0.9f
                }
            };

            // 2. Usar InferAsync directamente
            int tokenCount = 0;
            await foreach (var text in _executor.InferAsync(finalPrompt, inferenceParams))
            {
                Console.Write(text);
                fullContent.Append(text);
                tokenCount++;
            }

            Console.WriteLine($"\nTokens generados: {tokenCount}");

            // 3. Si respuesta vacía, reintentar
            if (fullContent.Length == 0)
            {
                Console.WriteLine("Respuesta vacía, reintentando...");
                inferenceParams.MaxTokens = 1024;
                inferenceParams.AntiPrompts = Array.Empty<string>();

                await foreach (var text in _executor.InferAsync(finalPrompt, inferenceParams))
                {
                    Console.Write(text);
                    fullContent.Append(text);
                    tokenCount++;
                }
            }

            var response = fullContent.ToString().Trim();

            // 4. Guardar en el historial manual
            _conversationHistory.Add(("user", input));
            _conversationHistory.Add(("assistant", response));

            // 5. Limpiar historial 
            if (_conversationHistory.Count > MaxHistoryPairs * 2 + 2)
            {
                Console.WriteLine($"Historial tenía {_conversationHistory.Count} entradas, limpiando...");
                _conversationHistory.RemoveRange(0, _conversationHistory.Count - MaxHistoryPairs * 2);
            }

            responseIA.Content = response;
            responseIA.IsSuccess = true;
            responseIA.DebugData.Add("ModelUsed", "Qwen-2.5-Coder-7B");
            responseIA.DebugData.Add("TimestampEnd", DateTime.UtcNow);
            responseIA.DebugData.Add("TokensGenerated", tokenCount);
            responseIA.DebugData.Add("ContentLength", response.Length);
            responseIA.DebugData.Add("HistorySize", _conversationHistory.Count);
        }
        catch (Exception ex)
        {
            responseIA.IsSuccess = false;
            responseIA.ErrorMessage = ex.Message;
            responseIA.Content = "Error durante el procesamiento.";
            Console.WriteLine($"\nError: {ex.Message}");
        }

        return responseIA;
    }

    public void ClearHistory()
    {
        _conversationHistory.Clear();
        Console.WriteLine("Historial manual limpiado.");
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing) { }

            _context?.Dispose();
            _model?.Dispose();
            _conversationHistory.Clear();

            Console.WriteLine("Memoria liberada.");
            _disposed = true;
        }
    }

    ~DevIAHelperFunctions() => Dispose(false);
}
