﻿using System.ComponentModel.DataAnnotations;

namespace BookSwap.DTOS
{
    public class ReaderSignUpDTO
    {
        public string ReaderName { get; set; }

        public string Password { get; set; }

        public string Email { get; set; }
        public string PhoneNumber { get; set; }
    }
}
