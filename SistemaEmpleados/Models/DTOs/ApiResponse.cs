namespace SistemaEmpleados.Models.DTOs;

/// <summary>
/// Respuesta genérica sin datos adicionales
/// </summary>
public class ApiResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<string> Errors { get; set; } = new();

    public static ApiResponse Ok(string message = "Operación exitosa.")
        => new() { Success = true, Message = message };

    public static ApiResponse Fail(string message = "Ocurrió un error.",
        List<string>? errors = null)
        => new()
        {
            Success = false,
            Message = message,
            Errors = errors ?? new List<string>()
        };
}

/// <summary>
/// Respuesta genérica con datos adicionales tipados
/// </summary>
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }
    public List<string> Errors { get; set; } = new();

    public static ApiResponse<T> Ok(T data, string message = "Operación exitosa.")
        => new() { Success = true, Message = message, Data = data };

    public static ApiResponse<T> Fail(string message = "Ocurrió un error.",
        List<string>? errors = null)
        => new()
        {
            Success = false,
            Message = message,
            Errors = errors ?? new List<string>()
        };
}