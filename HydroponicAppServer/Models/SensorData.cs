using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace HydroponicAppServer.Models
{
    public class SensorData
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int GardenId { get; set; } // Nếu thực sự có thể null

        [Required]
        [StringLength(12)]
        public string UserId { get; set; }

        public double? Temperature { get; set; }
        public double? Humidity { get; set; }
        public double? WaterLevel { get; set; }
        public DateTime? Time { get; set; }

        // Navigation property
        [ForeignKey("UserId")]
        [JsonIgnore]
        public User User { get; set; }

        [ForeignKey("GardenId")]
        [JsonIgnore]
        public Garden? Garden { get; set; }
    }
}