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

        // GET: api/Garden/by-user/{userId}
        [HttpGet("by-user/{userId}")]
        public async Task<ActionResult<IEnumerable<GardenResponseDto>>> GetGardensByUser(string userId)
        {
            // Lấy các vườn active
            var activeGardens = await _context.Gardens
                .Where(g => g.UserId == userId &&
                    (!g.EndDate.HasValue || g.EndDate.Value.Date >= DateTime.Today))
                .OrderByDescending(g => g.Id)
                .ToListAsync();

            if (activeGardens.Count > 1)
            {
                // Giữ lại vườn có Id cao nhất, xóa các vườn active còn lại
                var keepGarden = activeGardens.First();
                var removeGardens = activeGardens.Skip(1).ToList();
                _context.Gardens.RemoveRange(removeGardens);
                await _context.SaveChangesAsync();
            }

            // Sau khi xóa, lấy lại danh sách vườn của user
            var gardens = await _context.Gardens
                .Where(g => g.UserId == userId)
                .OrderByDescending(g => g.Id)
                .ToListAsync();

            var result = gardens.Select(g => new GardenResponseDto
            {
                Id = g.Id,
                UserId = g.UserId,
                Name = g.Name,
                VegetableType = g.VegetableType,
                StartDate = g.StartDate,
                EndDate = g.EndDate
            });
            return Ok(result);
        }

        // POST: api/Garden
        [HttpPost]
        public async Task<ActionResult<GardenResponseDto>> PostGarden(GardenCreateDto gardenDto)
        {
            // Kiểm tra UserId có tồn tại không
            var user = await _context.Users.FindAsync(gardenDto.UserId);
            if (user == null)
            {
                return BadRequest(new { message = "UserId không tồn tại." });
            }

            // Tìm các vườn active của user
            var activeGardens = await _context.Gardens
                .Where(g => g.UserId == gardenDto.UserId &&
                    (!g.EndDate.HasValue || g.EndDate.Value.Date >= DateTime.Today))
                .OrderByDescending(g => g.Id)
                .ToListAsync();

            if (activeGardens.Count > 0)
            {
                // Giữ lại vườn có Id cao nhất, xóa các vườn còn lại
                var keepGarden = activeGardens.First();
                var removeGardens = activeGardens.Skip(1).ToList();
                _context.Gardens.RemoveRange(removeGardens);
                await _context.SaveChangesAsync();
            }

            var garden = new Garden
            {
                UserId = gardenDto.UserId,
                Name = gardenDto.Name,
                VegetableType = gardenDto.VegetableType,
                StartDate = gardenDto.StartDate,
                EndDate = gardenDto.EndDate
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

            var response = new GardenResponseDto
            {
                Id = garden.Id,
                UserId = garden.UserId,
                Name = garden.Name,
                VegetableType = garden.VegetableType,
                StartDate = garden.StartDate,
                EndDate = garden.EndDate
            };

            return CreatedAtAction(nameof(GetGarden), new { id = garden.Id }, response);
        }

        // Các hàm khác giữ nguyên

        // GET: api/Garden/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<GardenResponseDto>> GetGarden(int id)
        {
            var garden = await _context.Gardens.FindAsync(id);

            if (garden == null)
            {
                return NotFound(new { message = "Không tìm thấy vườn." });
            }

            var response = new GardenResponseDto
            {
                Id = garden.Id,
                UserId = garden.UserId,
                Name = garden.Name,
                VegetableType = garden.VegetableType,
                StartDate = garden.StartDate,
                EndDate = garden.EndDate
            };

            return Ok(response);
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

    public class GardenResponseDto
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public string Name { get; set; }
        public string VegetableType { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }
}
