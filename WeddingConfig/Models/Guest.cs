using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WeddingConfig.Models
{    public class Guest
    {
        public string? Code { get; set; }
        public string? Name { get; set; }
        public string? Email { get; set; }
        public string? Description { get; set; }
        public bool? HasOne { get; set; }
    }
}
