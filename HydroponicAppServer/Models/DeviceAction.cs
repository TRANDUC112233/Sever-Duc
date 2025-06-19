using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HydroponicAppServer.Models
{
    public class DeviceAction
    {
        [Key]
        public int Id { get; set; } // Khóa chính tự tăng

        [Required]
        [StringLength(12)]
        public string UserId { get; set; } // Khóa ngoại

        [StringLength(50)]
        public string Device { get; set; }

        [StringLength(50)]
        public string Action { get; set; }

        public DateTime? Time { get; set; } // Thời gian thực hiện (manual hoặc đã thực thi)

        [StringLength(20)]
        public string Type { get; set; }

        [StringLength(20)]
        public string Repeat { get; set; }

        // ------- THÊM TRƯỜNG MỚI CHO LẬP LỊCH -------
        public bool IsScheduled { get; set; } // true: là tác vụ lập lịch, false: thao tác tay

        public DateTime? ScheduledTime { get; set; } // Thời gian dự kiến sẽ thực hiện (nếu là lập lịch)

        [StringLength(20)]
        public string Status { get; set; } // "Pending", "Executed", "Cancelled",...
        // -------------------------------------------

        // Navigation property
        [ForeignKey("UserId")]
        public User User { get; set; }
    }
}