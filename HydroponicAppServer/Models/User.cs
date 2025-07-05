using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace HydroponicAppServer.Models
{
    public class User
    {
        [Key]
        [StringLength(12)]
        public string Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Username { get; set; }

        [Required]
        [StringLength(100)]
        public string Password { get; set; }

        [StringLength(50)]
        public string? Role { get; set; }

        public ICollection<Garden> Gardens { get; set; } = new List<Garden>();
        public ICollection<SensorData> SensorDatas { get; set; } = new List<SensorData>();
        public ICollection<DeviceAction> DeviceActions { get; set; } = new List<DeviceAction>();
    }
}