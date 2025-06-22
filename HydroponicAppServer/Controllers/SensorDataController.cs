using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HydroponicAppServer.Models;

namespace HydroponicAppServer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
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
            try
            {
                return await _context.SensorDatas
                    .OrderByDescending(sd => sd.Time)
                    .Take(150)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Server error", detail = ex.Message });
            }
        }

        // GET: api/SensorData/{id}
        [HttpGet("{id:int}")]
        public async Task<ActionResult> GetSensorData(int id)
        {
            try
            {
                if (id <= 0)
                    return BadRequest(new { error = "Id must be greater than 0" });

                var sensorData = await _context.SensorDatas.FindAsync(id);

                if (sensorData == null)
                {
                    return NotFound(new { error = $"No data found with id: {id}" });
                }

                return Ok(sensorData);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Server error", detail = ex.Message });
            }
        }

        // GET: api/SensorData/by-user/{userId}
        [HttpGet("by-user/{userId}")]
        public async Task<ActionResult> GetSensorDataByUser(string userId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userId))
                    return BadRequest(new { error = "UserId is required" });

                DateTime threeDaysAgo = DateTime.UtcNow.AddDays(-3);

                // Xoá các bản ghi cũ hơn 3 ngày (nếu có)
                var oldRecords = _context.SensorDatas
                    .Where(sd => sd.UserId == userId && sd.Time < threeDaysAgo);
                if (oldRecords.Any())
                {
                    _context.SensorDatas.RemoveRange(oldRecords);
                    await _context.SaveChangesAsync();
                }

                var datas = await _context.SensorDatas
                    .Where(sd => sd.UserId == userId && sd.Time >= threeDaysAgo)
                    .OrderByDescending(sd => sd.Time)
                    .Take(150)
                    .ToListAsync();

                if (datas.Count == 0)
                    return Ok(new List<SensorData>()); // Trả về mảng rỗng

                return Ok(datas);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Server error", detail = ex.Message });
            }
        }

        // GET: api/SensorData/latest/{userId}
        [HttpGet("latest/{userId}")]
        public ActionResult GetLatestSensorData(string userId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userId))
                    return BadRequest(new { error = "UserId is required" });

                DateTime threeDaysAgo = DateTime.UtcNow.AddDays(-3);

                var data = _context.SensorDatas
                    .Where(sd => sd.UserId == userId && sd.Time >= threeDaysAgo)
                    .OrderByDescending(sd => sd.Time)
                    .FirstOrDefault();

                if (data == null)
                    return NotFound(new { error = "No data found in last 3 days for this user" });

                return Ok(data);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Server error", detail = ex.Message });
            }
        }

        // POST: api/SensorData
        [HttpPost]
        public async Task<ActionResult> PostSensorData(SensorData sensorData)
        {
            try
            {
                if (sensorData == null || string.IsNullOrWhiteSpace(sensorData.UserId))
                    return BadRequest(new { error = "Invalid SensorData" });

                // Log đầu vào để debug
                Console.WriteLine($"[POST] Received SensorData: UserId={sensorData.UserId}, Temp={sensorData.Temperature}, Humidity={sensorData.Humidity}, Water={sensorData.WaterLevel}, Time={sensorData.Time}");

                // Xoá tự động các bản ghi cũ hơn 3 ngày trước khi thêm mới
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
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Server error", detail = ex.Message });
            }
        }

        // PUT: api/SensorData/{id}
        [HttpPut("{id:int}")]
        public async Task<IActionResult> PutSensorData(int id, SensorData sensorData)
        {
            if (id != sensorData.Id)
            {
                return BadRequest(new { error = "Id mismatch" });
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
                    return NotFound(new { error = $"No data found with id: {id}" });
                }
                else
                {
                    return StatusCode(500, new { error = "Concurrency error" });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Server error", detail = ex.Message });
            }

            return NoContent();
        }

        // DELETE: api/SensorData/{id}
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteSensorData(int id)
        {
            try
            {
                var sensorData = await _context.SensorDatas.FindAsync(id);
                if (sensorData == null)
                {
                    return NotFound(new { error = $"No data found with id: {id}" });
                }

                _context.SensorDatas.Remove(sensorData);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Server error", detail = ex.Message });
            }
        }

        private bool SensorDataExists(int id)
        {
            return _context.SensorDatas.Any(e => e.Id == id);
        }
    }
}
