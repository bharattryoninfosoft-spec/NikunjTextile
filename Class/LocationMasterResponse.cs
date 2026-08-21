using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NikunjTextile.Class
{
    public class LocationMasterResponse
    {
        public int Code { get; set; }
        public string Message { get; set; }
        public List<LocationMaster> listLocationMaster { get; set; }
    }
}