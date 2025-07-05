using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HydroponicAppServer.Models
{
    public class DeviceAction
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int GardenId { get; set; } 

        [Required]
        [StringLength(12)]
        public string UserId { get; set; }

        [StringLength(50)]
        public string Device { get; set; }

        [StringLength(50)]
        public string Action { get; set; }

        public DateTime? Time { get; set; }

        [StringLength(20)]
        public string Type { get; set; }

        [StringLength(20)]
        public string Repeat { get; set; }

        public bool IsScheduled { get; set; }
        public DateTime? ScheduledTime { get; set; }

        [StringLength(20)]
        public string Status { get; set; }

        // Navigation property
        [ForeignKey("UserId")]
        public User User { get; set; }

        [ForeignKey("GardenId")]
        public Garden? Garden { get; set; }
    }
}