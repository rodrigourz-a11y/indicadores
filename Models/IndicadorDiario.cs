using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Indicadores.Models
{
    public class IndicadorDiario
    {
        public string Codigo { get; set; } = "UF";
        public DateTime Fecha { get; set; }
        public decimal Valor { get; set; }
        public string Fuente { get; set; } = "SII";
    }
}