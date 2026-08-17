using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Hlouwa.Models
{

    // Owned type: stored inside Orders table
    [Owned]
    public class OrderAddressSnapshot
    {
        [MaxLength(160)]
        public string? FullName { get; set; }

        [MaxLength(30)]
        public string? Phone { get; set; }

        [MaxLength(180)]
        public string? Line1 { get; set; }

        [MaxLength(180)]
        public string? Line2 { get; set; }

        [MaxLength(80)]
        public string? City { get; set; }

        [MaxLength(20)]
        public string? PostalCode { get; set; }

        [MaxLength(80)]
        public string? Governorate { get; set; }

        [MaxLength(2)]
        public string CountryCode { get; set; } = "TN";
    }

}
