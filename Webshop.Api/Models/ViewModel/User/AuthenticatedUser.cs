﻿
namespace Webshop.Api.Models.ViewModel.User
{
    public class AuthenticatedUser
    {
        public int UserId { get; set; }
        public string Token { get; set; }
        public string Firstname { get; set; }
        public string Lastname { get; set; }
        public string Email { get; set; }
    }
}
