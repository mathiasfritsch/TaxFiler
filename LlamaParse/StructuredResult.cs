using System.Text.Json;

namespace LlamaParse;

public record StructuredResult
{
    public JsonElement[] ResultPagesStructured { get; init; }
}