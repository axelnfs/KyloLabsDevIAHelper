using KyloLabs.DevIAHelper.Core;
using KyloLabs.DevIAHelper.Core.Models;
using LLama;
using LLama.Common;
using LLama.Sampling;
using System;
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

    public DevIAHelperFunctions(string modelName = "qwen2.5-coder-7b-instruct-q3_k_m.gguf")
    {
        _modelPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, modelName);

        try
        {
            _memoryLoader = new PermanentMemoryLoader();
            Console.WriteLine("📖 Memoria permanente cargada exitosamente.");
        }
        catch (FileNotFoundException ex)
        {
            Console.WriteLine($"⚠️ {ex.Message}");
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

        // El History ya existe, simplemente agregamos mensajes
        string systemPrompt;
        if (_memoryLoader != null)
        {
            systemPrompt = _memoryLoader.GenerateSystemPrompt();
            Console.WriteLine("Inyectando personalidad de Silicia...");
        }
        else
        {
            systemPrompt = "Eres un asistente experto en programación C# y desarrollo de software. " +
                           "Responde siempre en español, de manera clara y con ejemplos de código.";
            Console.WriteLine("Usando system prompt por defecto (sin personalidad).");
        }

        // Agregar System Prompt
        _session.History.AddMessage(AuthorRole.System, systemPrompt);

        // Agregar un mensaje de ejemplo para que el modelo aprenda el formato
        _session.History.AddMessage(AuthorRole.User, "Preséntate brevemente.");
        _session.History.AddMessage(AuthorRole.Assistant,
            "¡Hola! Soy Silicia, tu asistente IA especializada en programación. " +
            "Estoy aquí para ayudarte con código C#, depuración y cualquier duda técnica. " +
            "¿En qué puedo ayudarte hoy?");

        // Mostrar el historial para depuración
        Console.WriteLine("HISTORIAL INICIAL");
        foreach (var msg in _session.History.Messages) // o .ToList()
        {
            var contentPreview = msg.Content.Length > 100
                ? msg.Content[..100] + "..."
                : msg.Content;
            Console.WriteLine($"[{msg.AuthorRole}]: {contentPreview}");
        }

        Console.WriteLine("Modelo Qwen cargado y listo.");
    }

    public async Task<ResponseIA> InputAsync(string input)
    {
        var responseIA = new ResponseIA();
        var fullContent = new StringBuilder();

        Console.WriteLine($"\nProcesando input: \"{(input.Length > 50 ? input[..50] + "..." : input)}\"");

        if (_context == null)
        {
            Console.WriteLine("Inicializando modelo por primera vez...");
            InicializarModelo();
        }

        try
        {
            var message = new ChatHistory.Message(AuthorRole.User, input);

            var inferenceParams = new InferenceParams
            {
                MaxTokens = 512,
                AntiPrompts = new[] { "<|im_end|>" }, // Solo <|im_end|> como anti-prompt
                SamplingPipeline = new DefaultSamplingPipeline()
                {
                    Temperature = 0.7f,
                    TopP = 0.9f
                }
            };

            int tokenCount = 0;
            await foreach (var text in _session.ChatAsync(message, inferenceParams))
            {
                Console.Write(text);
                fullContent.Append(text);
                tokenCount++;
            }

            Console.WriteLine($"\nTokens generados: {tokenCount}");

            // Si no se generó contenido, reintentar con más tokens y sin anti-prompts
            if (fullContent.Length == 0)
            {
                Console.WriteLine("Respuesta vacía detectada, reintentando...");

                inferenceParams.MaxTokens = 1024;
                inferenceParams.AntiPrompts = Array.Empty<string>();

                await foreach (var text in _session.ChatAsync(message, inferenceParams))
                {
                    Console.Write(text);
                    fullContent.Append(text);
                    tokenCount++;
                }

                Console.WriteLine($"\nTokens generados en reintento: {tokenCount}");
            }

            responseIA.Content = fullContent.ToString().Trim();
            responseIA.IsSuccess = true;

            responseIA.DebugData.Add("ModelUsed", "Qwen-2.5-Coder-7B");
            responseIA.DebugData.Add("TimestampEnd", DateTime.UtcNow);
            responseIA.DebugData.Add("TokensGenerated", tokenCount);
            responseIA.DebugData.Add("ContentLength", fullContent.Length);
        }
        catch (Exception ex)
        {
            responseIA.IsSuccess = false;
            responseIA.ErrorMessage = ex.Message;
            responseIA.Content = "Error durante el procesamiento.";
            Console.WriteLine($"\nError: {ex.Message}");
            Console.WriteLine($"StackTrace: {ex.StackTrace}");
        }

        return responseIA;
    }

    //public void ClearHistory()
    //{
    //    if (_context != null)
    //    {
    //        _executor = new InteractiveExecutor(_context);
    //        _session = new ChatSession(_executor);

    //        // Re-agregar el system prompt
    //        _session.History.AddMessage(AuthorRole.System,
    //            "Eres un asistente experto en programación C# y desarrollo de software. " +
    //            "Responde siempre en español, de manera clara y con ejemplos de código.");

    //        Console.WriteLine("Historial limpiado (nueva sesión creada)");
    //    }
    //}

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                // No intentamos limpiar History, solo liberamos recursos no administrados
            }

            _context?.Dispose();
            _model?.Dispose();

            Console.WriteLine("Memoria del modelo liberada correctamente.");
            _disposed = true;
        }
    }

    ~DevIAHelperFunctions() => Dispose(false);
}

