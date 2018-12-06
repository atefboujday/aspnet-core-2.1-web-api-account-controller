using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AspNetCoreWebApp.Models
{
    public class UserAccessModel
    {
        public UserAccessModel(string fullName, string token, DateTime expirationDate)
        {
            FullName = fullName;
            Token = token;
            ExpirationDate = expirationDate;
        }
        public string FullName { get; set; }
        public string Token { get; set; }
        public DateTime ExpirationDate { get; set; }
    }
}
