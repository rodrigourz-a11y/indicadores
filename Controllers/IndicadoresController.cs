using Indicadores.Models;
using Indicadores.Services;
using Microsoft.AspNetCore.Mvc;

namespace Indicadores.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class IndicadoresController : ControllerBase
    {
        private readonly SiiScraperService _sii;
        private readonly PreviredScraperService _previred;
        private readonly IndicadorRepository _repo;

        public IndicadoresController(
            SiiScraperService sii,
            PreviredScraperService previred,
            IndicadorRepository repo)
        {
            _sii = sii;
            _previred = previred;
            _repo = repo;
        }

        // Endpoint de salud para Railway: debe ser mínimo y rápido
        [HttpGet("disponibles")]
        public IActionResult Disponibles()
        {
            return Ok(new { status = "OK" });
        }

        // Obtener un indicador específico
        [HttpGet("{codigo}/{fecha:datetime}")]
        public async Task<ActionResult> Obtener(string codigo, DateTime fecha)
        {
            var valor = await _repo.ObtenerValor(codigo.ToUpper(), fecha.Date);
            if (valor.HasValue)
                return Ok(new { fecha = fecha.Date, valor = valor.Value });
            return NotFound();
        }

        // Actualizar todos los indicadores
        [HttpPost("actualizar")]
        public async Task<ActionResult> Actualizar()
        {
            var hoy = DateTime.Today;
            var todos = new List<IndicadorDiario>();

            // UF diaria (2 años)
            foreach (int anio in new[] { hoy.Year, hoy.Year - 1 })
                todos.AddRange(await _sii.ObtenerUfDiaria(anio));

            // Indicadores desde Previred
            todos.AddRange(await _previred.ObtenerIndicadores());

            if (todos.Count == 0)
                return BadRequest("No se obtuvieron datos.");

            await _repo.GuardarIndicadores(todos);
            var resumen = todos.GroupBy(x => x.Codigo).ToDictionary(g => g.Key, g => g.Count());
            return Ok(new { total = todos.Count, detalles = resumen });
        }

        // Redirigir a donaciones
        [HttpGet("donar")]
        public IActionResult Donar()
        {
            return Redirect("https://www.buymeacoffee.com/indicadoreschile");
        }
    }
}