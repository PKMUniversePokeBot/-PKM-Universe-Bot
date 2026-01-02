// PKM Universe Bot - API Response Model
// Written by PKM Universe - 2025

namespace PKMUniverse.Web.Models;

public class ApiResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public object? Data { get; set; }
}
