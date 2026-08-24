using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace NikunjTextile.Class
{
    public class JobberViewModel
    {
        public int Id { get; set; }
        public long CompanyId { get; set; }
        public long UserAccountId { get; set; }
        public DateTime DateAndTime { get; set; }

        [Required]
        [Display(Name = "Jobber Type")]
        public string JobberType { get; set; } = "Weaving / Process";

        public string Gstin { get; set; }
        public string PanNo { get; set; }

        [Required(ErrorMessage = "Party Name is required")]
        public string PartyName { get; set; }

        public string Prefix { get; set; }

        [Required(ErrorMessage = "Mobile Number is required")]
        public string MobileNumber { get; set; }

        public string AlternateNumber { get; set; }
        public string Email { get; set; }
        public string PlaceOfSupply { get; set; }
        public string Pincode { get; set; }
        public string City { get; set; }

        // Bank Details
        public string BankHolderName { get; set; }
        public string BankAcNo { get; set; }
        public string BankName { get; set; }
        public string BankIfsc { get; set; }
        public string BankBranch { get; set; }

        // Address Details
        public string BillingAddress { get; set; }

        // Machine Details
        public string MachineType { get; set; }
        public int Rpm { get; set; }
        public int PanaRepeat { get; set; }
        public int PanaWidth { get; set; }
        public int NoOfMachines { get; set; }

        public byte IsActiveParty { get; set; } = 1;
    }
}