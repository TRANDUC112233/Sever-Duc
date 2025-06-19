using HydroponicAppServer.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace HydroponicAppServer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DeviceActionController : ControllerBase
    {
        private readonly AppDbContext _context;

        public DeviceActionController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/DeviceAction
        [HttpGet]
        public async Task<ActionResult<IEnumerable<DeviceAction>>> GetDeviceActions()
        {
            return await _context.DeviceActions.ToListAsync();
        }

        // GET: api/DeviceAction/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<DeviceAction>> GetDeviceAction(int id)
        {
            var deviceAction = await _context.DeviceActions.FindAsync(id);

            if (deviceAction == null)
            {
                return NotFound();
            }

            return deviceAction;
        }

        // GET: api/DeviceAction/by-user/{userId}
        [HttpGet("by-user/{userId}")]
        public async Task<ActionResult<IEnumerable<DeviceAction>>> GetDeviceActionsByUser(string userId)
        {
            return await _context.DeviceActions
                .Where(da => da.UserId == userId)
                .ToListAsync();
        }

        // GET: api/DeviceAction/scheduled
        [HttpGet("scheduled")]
        public async Task<ActionResult<IEnumerable<DeviceAction>>> GetScheduledDeviceActions()
        {
            return await _context.DeviceActions
                .Where(da => da.IsScheduled)
                .ToListAsync();
        }

        // GET: api/DeviceAction/scheduled/pending
        [HttpGet("scheduled/pending")]
        public async Task<ActionResult<IEnumerable<DeviceAction>>> GetPendingScheduledDeviceActions()
        {
            return await _context.DeviceActions
                .Where(da => da.IsScheduled && da.Status == "Pending")
                .ToListAsync();
        }

        // POST: api/DeviceAction
        [HttpPost]
        public async Task<ActionResult<DeviceAction>> PostDeviceAction(DeviceAction deviceAction)
        {
            // Nếu là lập lịch thì mặc định status là Pending, nếu chưa khai báo
            if (deviceAction.IsScheduled && string.IsNullOrEmpty(deviceAction.Status))
            {
                deviceAction.Status = "Pending";
            }
            // Nếu không phải lập lịch thì đã thực hiện ngay, gán status Executed nếu chưa có
            if (!deviceAction.IsScheduled && string.IsNullOrEmpty(deviceAction.Status))
            {
                deviceAction.Status = "Executed";
            }

            _context.DeviceActions.Add(deviceAction);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetDeviceAction), new { id = deviceAction.Id }, deviceAction);
        }

        // PUT: api/DeviceAction/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> PutDeviceAction(int id, DeviceAction deviceAction)
        {
            if (id != deviceAction.Id)
            {
                return BadRequest();
            }

            _context.Entry(deviceAction).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!DeviceActionExists(id))
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

        // PATCH: api/DeviceAction/{id}/executed
        [HttpPatch("{id}/executed")]
        public async Task<IActionResult> MarkDeviceActionExecuted(int id)
        {
            var deviceAction = await _context.DeviceActions.FindAsync(id);

            if (deviceAction == null)
            {
                return NotFound();
            }

            // Đánh dấu đã thực thi (cho lập lịch)
            deviceAction.Status = "Executed";
            deviceAction.Time = System.DateTime.Now;

            _context.Entry(deviceAction).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // PATCH: api/DeviceAction/{id}/cancel
        [HttpPatch("{id}/cancel")]
        public async Task<IActionResult> CancelDeviceAction(int id)
        {
            var deviceAction = await _context.DeviceActions.FindAsync(id);

            if (deviceAction == null)
            {
                return NotFound();
            }

            // Đánh dấu đã huỷ (cho lập lịch)
            deviceAction.Status = "Cancelled";

            _context.Entry(deviceAction).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE: api/DeviceAction/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteDeviceAction(int id)
        {
            var deviceAction = await _context.DeviceActions.FindAsync(id);
            if (deviceAction == null)
            {
                return NotFound();
            }

            _context.DeviceActions.Remove(deviceAction);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool DeviceActionExists(int id)
        {
            return _context.DeviceActions.Any(e => e.Id == id);
        }
    }
}