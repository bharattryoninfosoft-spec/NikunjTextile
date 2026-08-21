using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NikunjTextile.Class
{
    public class PartyMasterResponse
    {
        public int Code { get; set; }
        public string Message { get; set; }
        public List<PartyMaster> listPartyMaster { get; set; }

    }
}