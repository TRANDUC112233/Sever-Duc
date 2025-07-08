using HydroponicAppServer.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System;

namespace HydroponicAppServer.Controllers
{
    // KHÔNG khai báo lại interface ở đây, chỉ sử dụng thôi!
    [Route("api/[controller]")]
    [ApiController]
    public class SensorDataController : ControllerBase
    {
        private readonly AppDbContext _context;

        public SensorDataController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/SensorData
        [HttpGet]
        public async Task<ActionResult<IEnumerable<SensorData>>> GetSensorData()
        {
            return await _context.SensorDatas
                .OrderByDescending(sd => sd.Time)
                .Take(200)
                .ToListAsync();
        }

        // GET: api/SensorData/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<SensorData>> GetSensorData(int id)
        {
            var sensorData = await _context.SensorDatas.FindAsync(id);

            if (sensorData == null)
            {
                return NotFound();
            }

            return sensorData;
        }

        // GET: api/SensorData/by-user/{userId}
        [HttpGet("by-user/{userId}")]
        public async Task<ActionResult<IEnumerable<SensorData>>> GetSensorDataByUser(string userId)
        {
            return await _context.SensorDatas
                .Where(sd => sd.UserId == userId)
                .ToListAsync();
        }

        // POST: api/SensorData
        [HttpPost]
        public async Task<ActionResult<SensorData>> PostSensorData(SensorData sensorData)
        {
            return BadRequest("❌ Ghi dữ liệu cảm biến không được phép qua API. Vui lòng sử dụng hệ thống nền tự động.");
        }

        // PUT: api/SensorData/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> PutSensorData(int id, SensorData sensorData)
        {
            return BadRequest("❌ Chỉnh sửa dữ liệu cảm biến không được phép qua API.");
        }

        // DELETE: api/SensorData/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSensorData(int id)
        {
            var sensorData = await _context.SensorDatas.FindAsync(id);
            if (sensorData == null)
            {
                return NotFound();
            }

            _context.SensorDatas.Remove(sensorData);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool SensorDataExists(int id)
        {
            return _context.SensorDatas.Any(e => e.Id == id);
        }

        // GET: api/SensorData/cache/{userId}
        [HttpGet("cache/{userId}")]
        public ActionResult<SensorData> GetCacheSensor(
            string userId,
            [FromServices] IMqttSensorCache cache)
        {
            var data = cache.GetLatestSensor(userId);
            if (data == null)
                return NotFound();
            return Ok(data);
        }

        // GET: api/SensorData/cache
        [HttpGet("cache")]
        public ActionResult<IEnumerable<SensorData>> GetAllCacheSensor(
            [FromServices] IMqttSensorCache cache)
        {
            var allData = cache.GetAll();
            return Ok(allData);
        }
    }
}