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
    public class UserController : ControllerBase
    {
        private readonly AppDbContext _context;

        public UserController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/User
        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserDto>>> GetUsers()
        {
            // Trả về DTO chỉ chứa trường cần thiết, tránh lồng navigation property lớn
            return await _context.Users
                .Select(u => new UserDto
                {
                    Id = u.Id,
                    Username = u.Username,
                    Password = u.Password,
                    Role = u.Role,
                    GardensCount = u.Gardens.Count,
                    SensorDatasCount = u.SensorDatas.Count,
                    DeviceActionsCount = u.DeviceActions.Count
                })
                .ToListAsync();
        }

        // GET: api/User/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<UserDetailDto>> GetUser(string id)
        {
            // Trả về DTO với thông tin chi tiết user + danh sách garden phẳng
            var user = await _context.Users
                .Where(u => u.Id == id)
                .Select(u => new UserDetailDto
                {
                    Id = u.Id,
                    Username = u.Username,
                    Password = u.Password,
                    Role = u.Role,
                    Gardens = u.Gardens.Select(g => new GardenLiteDto
                    {
                        Id = g.Id,
                        Name = g.Name,
                        VegetableType = g.VegetableType,
                        StartDate = g.StartDate,
                        EndDate = g.EndDate
                    }).ToList(),
                    // Nếu cần bạn có thể trả về các trường khác tương tự
                })
                .FirstOrDefaultAsync();

            if (user == null)
            {
                return NotFound();
            }
            return user;
        }

        // POST: api/User
        [HttpPost]
        public async Task<ActionResult<User>> PostUser(User user)
        {
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetUser), new { id = user.Id }, user);
        }

        // PUT: api/User/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> PutUser(string id, User user)
        {
            if (id != user.Id)
            {
                return BadRequest();
            }
_context.Entry(user).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!UserExists(id))
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

        // DELETE: api/User/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(string id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool UserExists(string id)
        {
            return _context.Users.Any(e => e.Id == id);
        }

        // DTOs
        public class UserDto
        {
            public string Id { get; set; }
            public string Username { get; set; }
            public string Password { get; set; }
            public string Role { get; set; }
            public int GardensCount { get; set; }
            public int SensorDatasCount { get; set; }
            public int DeviceActionsCount { get; set; }
        }

        public class UserDetailDto
        {
            public string Id { get; set; }
            public string Username { get; set; }
            public string Password { get; set; }
            public string Role { get; set; }
            public List<GardenLiteDto> Gardens { get; set; }
        }

        public class GardenLiteDto
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public string VegetableType { get; set; }
            public DateTime StartDate { get; set; }
            public DateTime? EndDate { get; set; }
        }
    }
}
