using System;

namespace NikunjTextile.Class
{
    public class UserSecurityContext
    {
        public long UserAccountId { get; set; }
        public string UserRole { get; set; }
        public bool IsAdmin { get; set; }
        public bool IsJobber { get; set; }
        public long LoggedInJobberID { get; set; }
        public string JobberName { get; set; }
        public long CompanyId { get; set; }
    }
}
