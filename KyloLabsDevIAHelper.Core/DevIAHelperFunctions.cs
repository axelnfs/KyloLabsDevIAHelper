using KyloLabs.DevIAHelper.Core;
using KyloLabs.DevIAHelper.Core.Models;
using LLama;
using LLama.Common;
using LLama.Sampling;
using System.Text;

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

        if (!File.Exists(_modelPath))
        {
            throw new FileNotFoundException($"No se encontró el modelo de IA (.gguf) en la ruta: {_modelPath}");
        }

        _memoryLoader = new PermanentMemoryLoader();
        Console.WriteLine("Estructura de memoria e identidad de Silicia cargada correctamente.");
    }

    public void InicializarModelo()
    {
        Console.WriteLine("Cargando tensores del modelo en memoria RAM (CPU)");

        var parameters = new ModelParams(_modelPath)
        {
            //Nota: Esto en front lo podría editar el usuario a su gusto
            ContextSize = 8192,
            GpuLayerCount = 0, // Inferencia pura por CPU
            Threads = 8        // Ajuste óptimo para los 8 núcleos físicos del procesador
        };

        _model = LLamaWeights.LoadFromFile(parameters);
        _context = _model.CreateContext(parameters);
        _executor = new InteractiveExecutor(_context);
        _session = new ChatSession(_executor);

        Console.WriteLine("Estructurando e inyectando la identidad de Silicia...");
        string systemPrompt = _memoryLoader.GenerateSystemPrompt();

        // 3. Establecemos las directivas iniciales en la sesión viva de LLamaSharp
        _session.History.AddMessage(AuthorRole.System, systemPrompt);

        Console.WriteLine("Sistema Silicia inicializado y listo para operar.");
        Console.WriteLine($"Tamaño del System Prompt: {systemPrompt.Length} caracteres.");
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
            int maxMessagesAllowed = (MaxHistoryPairs * 2) + 1;
            while (_session.History.Messages.Count > maxMessagesAllowed)
            {
                // Removemos el mensaje más viejo del historial (posición 1, justo después del System Prompt)
                Console.WriteLine("Truncando historial antiguo de la sesión para optimizar RAM...");
                _session.History.Messages.RemoveAt(1);
            }


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

            Console.WriteLine("Procesando inferencia en CPU...");
            int tokenCount = 0;

            // 3. El ChatSession se encarga del formato ChatML de forma nativa y aprovecha el KV Cache.
            // Solo le pasamos el "input" del usuario, NO todo el string acumulado.
            await foreach (var text in _session.ChatAsync(new ChatHistory.Message(AuthorRole.User, input), inferenceParams))
            {
                Console.Write(text);
                fullContent.Append(text);
                tokenCount++;
            }

            // 3. Si respuesta vacía, reintentar
            if (fullContent.Length == 0)
            {
                Console.WriteLine("Respuesta vacía, reintentando...");
                inferenceParams.MaxTokens = 1024;

                await foreach (var text in _session.ChatAsync(new ChatHistory.Message(AuthorRole.User, "Por favor, expande tu respuesta anterior."), inferenceParams))
                {
                    Console.Write(text);
                    fullContent.Append(text);
                    tokenCount++;
                }
            }

            var response = fullContent.ToString().Trim();
            Console.WriteLine($"\nTokens generados: {tokenCount}");

            // 5. Llenado de Metadata de salida
            responseIA.Content = response;
            responseIA.IsSuccess = true;
            responseIA.DebugData.Add("ModelUsed", "Qwen-2.5-Coder-7B");
            responseIA.DebugData.Add("TimestampEnd", DateTime.UtcNow);
            responseIA.DebugData.Add("TokensGenerated", tokenCount);
            responseIA.DebugData.Add("ContentLength", response.Length);
            responseIA.DebugData.Add("HistorySize", _session.History.Messages.Count);
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
        // Si la sesión ya existe, limpiamos sus mensajes internos
        if (_session?.History?.Messages != null)
        {
            _session.History.Messages.Clear();

            // IMPORTANTE: Al limpiar el historial de chat, debemos volver a inyectar 
            // el System Prompt para que Silicia no olvide quién es en la siguiente pregunta.
            string systemPrompt = _memoryLoader.GenerateSystemPrompt();
            _session.History.AddMessage(AuthorRole.System, systemPrompt);
        }

        Console.WriteLine("Historial de conversación de Silicia reiniciado con éxito.");
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this); // Le dice al recolector de basura que ya limpiamos los recursos manualmente
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                // Liberar recursos administrados si los hubiera (en esta fase no hay colecciones pesadas aquí)
            }

            // LIBERACIÓN DE MEMORIA NO ADMINISTRADA (Los tensores en RAM)
            // Usamos el operador elvis (?.) para evitar NullReferenceException si se llama antes de inicializar
            _context?.Dispose();
            _model?.Dispose();

            Console.WriteLine("Tensores del modelo .gguf descargados de la RAM. Memoria liberada con éxito.");
            _disposed = true;
        }
    }

    // Destructor/Finalizador por si al desarrollador se le olvida usar el bloque 'using' o llamar a Dispose()
    ~DevIAHelperFunctions() => Dispose(false);
}
