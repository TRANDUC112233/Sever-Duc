using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HydroponicAppServer.Models
{
    public class Garden
    {
        [Key]
        public int Id { get; set; } // Để non-nullable

        [Required]
        [StringLength(12)]
        public string UserId { get; set; } // FK tới User

        [StringLength(100)]
        public string Name { get; set; }

        [StringLength(50)]
        public string VegetableType { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        // Navigation
        [ForeignKey("UserId")]
        public User User { get; set; }
        public ICollection<SensorData> SensorDatas { get; set; } = new List<SensorData>();
        public ICollection<DeviceAction> DeviceActions { get; set; } = new List<DeviceAction>();
    }
}