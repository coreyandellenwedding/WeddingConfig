using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WeddingConfig.Models
{
    internal class UserResponse
    {
        public bool IsConfirmed { get; set; }
        public string? Name { get; set; }
        public string? Email { get; set; }
        public bool? HasOne { get; set; }
        public string? Description { get; set; }
    }
}
