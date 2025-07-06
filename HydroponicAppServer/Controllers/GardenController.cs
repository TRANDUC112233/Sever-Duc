using HydroponicAppServer.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HydroponicAppServer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GardenController : ControllerBase
    {
        private readonly AppDbContext _context;

        public GardenController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/Garden
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Garden>>> GetGardens()
        {
            return await _context.Gardens
                .Include(g => g.User)
                .Include(g => g.SensorDatas)
                .Include(g => g.DeviceActions)
                .ToListAsync();
        }

        // GET: api/Garden/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<Garden>> GetGarden(int id)
        {
            var garden = await _context.Gardens
                .Include(g => g.User)
                .Include(g => g.SensorDatas)
                .Include(g => g.DeviceActions)
                .FirstOrDefaultAsync(g => g.Id == id);

            if (garden == null)
            {
                return NotFound();
            }

            return garden;
        }

        // GET: api/Garden/by-user/{userId}
        [HttpGet("by-user/{userId}")]
        public async Task<ActionResult<IEnumerable<Garden>>> GetGardensByUser(string userId)
        {
            return await _context.Gardens
                .Where(g => g.UserId == userId)
                .Include(g => g.SensorDatas)
                .Include(g => g.DeviceActions)
                .ToListAsync();
        }

        // POST: api/Garden
        [HttpPost]
        public async Task<ActionResult<Garden>> PostGarden(Garden garden)
        {
            _context.Gardens.Add(garden);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetGarden), new { id = garden.Id }, garden);
        }

        // PUT: api/Garden/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> PutGarden(int id, Garden garden)
        {
            if (id != garden.Id)
            {
                return BadRequest();
            }

            _context.Entry(garden).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!GardenExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // DELETE: api/Garden/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteGarden(int id)
        {
            var garden = await _context.Gardens.FindAsync(id);
            if (garden == null)
            {
                return NotFound();
            }

            _context.Gardens.Remove(garden);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool GardenExists(int id)
        {
            return _context.Gardens.Any(e => e.Id == id);
        }
    }
}