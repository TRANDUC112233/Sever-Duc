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
                return NotFound(new { message = "Không tìm thấy vườn." });
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
        public async Task<ActionResult<Garden>> PostGarden(GardenCreateDto gardenDto)
        {
            // Kiểm tra UserId có tồn tại không
            var user = await _context.Users.FindAsync(gardenDto.UserId);
            if (user == null)
            {
                return BadRequest(new { message = "UserId không tồn tại." });
            }

            var garden = new Garden
            {
                UserId = gardenDto.UserId,
                Name = gardenDto.Name,
                VegetableType = gardenDto.VegetableType,
                StartDate = gardenDto.StartDate,
                EndDate = gardenDto.EndDate
                // Id sẽ được tự tăng bởi DB
            };

            _context.Gardens.Add(garden);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                return BadRequest(new { message = "Lỗi khi lưu vườn: " + ex.Message });
            }

            return CreatedAtAction(nameof(GetGarden), new { id = garden.Id }, garden);
        }
        // PUT: api/Garden/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> PutGarden(int id, GardenCreateDto gardenDto)
        {
            var garden = await _context.Gardens.FindAsync(id);
            if (garden == null)
            {
                return NotFound(new { message = "Không tìm thấy vườn để cập nhật." });
            }

            garden.Name = gardenDto.Name;
            garden.VegetableType = gardenDto.VegetableType;
            garden.StartDate = gardenDto.StartDate;
            garden.EndDate = gardenDto.EndDate;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!GardenExists(id))
                {
                    return NotFound(new { message = "Không tìm thấy vườn để cập nhật." });
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
                return NotFound(new { message = "Không tìm thấy vườn để xóa." });
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

    // DTO chỉ gửi các trường cần thiết
    public class GardenCreateDto
    {
        public string UserId { get; set; }
        public string Name { get; set; }
        public string VegetableType { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }
}
