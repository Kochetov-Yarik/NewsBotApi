using Microsoft.AspNetCore.Mvc;
using NewsBotApi.Models;
using NewsBotApi.Services;
using System.Threading.Tasks;

namespace NewsBotApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FavoritesController : ControllerBase
    {
        private readonly DatabaseService _dbService;
        public FavoritesController(DatabaseService dbService)
        {
            _dbService = dbService;
        }

        [HttpPost]
        public async Task<IActionResult> AddSite([FromBody] FavoriteSite site)
        {
            await _dbService.AddSiteAsync(site);
            return Ok("Сайт успішно додано до бази даних!");
        }

        [HttpGet]
        public async Task<IActionResult> GetSites()
        {
            var sites = await _dbService.GetSitesAsync();
            return Ok(sites);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSite(int id)
        {
            await _dbService.DeleteSiteAsync(id);
            return Ok($"Сайт з ID {id} успішно видалено!");
        }
    }
}