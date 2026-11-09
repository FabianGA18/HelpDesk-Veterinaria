namespace Sistema_Gestión_Citas.Services
{
    /// <summary>Resultado de una operación de negocio: éxito o mensaje de error para el usuario.</summary>
    public record Resultado(bool Exito, string? Error = null)
    {
        public static Resultado Ok() => new(true);
        public static Resultado Falla(string error) => new(false, error);
    }

    public record Resultado<T>(bool Exito, T? Valor, string? Error = null)
    {
        public static Resultado<T> Ok(T valor) => new(true, valor);
        public static Resultado<T> Falla(string error) => new(false, default, error);
    }
}
