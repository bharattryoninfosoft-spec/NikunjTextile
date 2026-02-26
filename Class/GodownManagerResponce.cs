using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NikunjTextile.Class
{
    public class GodownManagerResponce
    {
        public int Code { get; set; }
        public string Message { get; set; }
        public List<GodownManager> listGodownManager { get; set; }

    }
}