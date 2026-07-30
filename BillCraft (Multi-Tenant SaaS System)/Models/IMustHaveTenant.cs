using System;

namespace BillCraft.Web.Models
{
    // যেসব মডেল টেন্যান্টের অধীনে থাকবে তারা এই ইন্টারফেস ব্যবহার করবে
    public interface IMustHaveTenant
    {
        string TenantId { get; set; }
    }

    // প্রতিটা টেবিলে তৈরি ও আপডেটের সময় ট্র্যাক রাখার জন্য বেস ক্লাস
    public abstract class BaseEntity
    {
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public string? UpdatedBy { get; set; }
    }
}