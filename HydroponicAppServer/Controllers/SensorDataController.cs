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
                .Take(100)
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
            Console.WriteLine($"[POST] Received SensorData: UserId={sensorData.UserId}, Temp={sensorData.Temperature}, Humidity={sensorData.Humidity}, Water={sensorData.WaterLevel}, Time={sensorData.Time}");

            var threeDaysAgo = DateTime.UtcNow.AddDays(-3);
            var oldRecords = await _context.SensorDatas
                .Where(sd => sd.Time != null && sd.Time < threeDaysAgo)
                .ToListAsync();

            if (oldRecords.Any())
            {
                _context.SensorDatas.RemoveRange(oldRecords);
                await _context.SaveChangesAsync();
            }

            _context.SensorDatas.Add(sensorData);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetSensorData), new { id = sensorData.Id }, sensorData);
        }

        // PUT: api/SensorData/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> PutSensorData(int id, SensorData sensorData)
        {
            if (id != sensorData.Id)
            {
                return BadRequest();
            }
            _context.Entry(sensorData).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!SensorDataExists(id))
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