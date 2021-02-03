using System;
using System.Collections.Generic;
using System.Text;

namespace PickleTrick.LoginServer.Data
{
    public class DbUser
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public long AuthToken { get; set; }
    }
}
