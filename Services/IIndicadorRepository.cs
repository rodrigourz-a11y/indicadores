using Indicadores.Models;

namespace Indicadores.Services
{
    // IIndicadorRepository.cs
    public interface IIndicadorRepository
    {
        Task<decimal?> ObtenerValor(string codigo, DateTime fecha);
        Task GuardarIndicadores(List<IndicadorDiario> indicadores);
    }
}