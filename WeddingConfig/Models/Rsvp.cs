using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WeddingConfig.Models
{
    public class Rsvp
    {
        public DateTime Date { get; set; }
        public string? Code { get; set; }
        public string? Name { get; set; }
        public string? Email { get; set; }
        public string? PlusOne { get; set; }
        public string? Attendance { get; set; }
    }
}
