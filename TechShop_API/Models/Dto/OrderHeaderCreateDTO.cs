﻿using System.ComponentModel.DataAnnotations;

namespace TechShop_API.Models.Dto
{
    public class OrderHeaderCreateDTO
    {
        [Required]
        public string PickupName { get; set; }
        [Required]
        public string PickupPhoneNumber { get; set; }
        [Required]
        public string PickupEmail { get; set; }

        public string ApplicationUserId { get; set; }
        public decimal OrderTotal { get; set; }


        public string StripePaymentIntentID { get; set; }
        public string Status { get; set; }
        public int TotalItems { get; set; }

        public IEnumerable<OrderDetailCreateDTO> OrderDetailsDTO { get; set; }
    }
}
