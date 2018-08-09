using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using SmartStore.Core.Domain.Catalog;

namespace SmartStore.Core.Domain.Shipping
{
    public  class ShippingAddress
    {
        [Required]
        public string Name { get; set; }
        //[Required]
        //public string LastName { get; set; }
        [Required]
        [RegularExpression("^[a-z0-9_\\+-]+(\\.[a-z0-9_\\+-]+)*@[a-z0-9-]+(\\.[a-z0-9]+)*\\.([a-z]{2,4})$", ErrorMessage = "Invalid email format.")]
        public string Email { get; set; }

        [Required]
        //[DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:yyyy-MM-ddThh:mm:ss}")]
        [Display(Name = "Delivery Date and Time")]
        public string DeliveryDateTime { get; set; }
        //[DataType(DataType.Date)]
        //[DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]


        [Required]
        [RegularExpression("([0-9]{10,10}$)", ErrorMessage = "Please enter valid Phone Number")]
        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; }

        [Display(Name = "Address")]
        public string Address { get; set; }

        public string Budget { get; set; }

        //[RegularExpression("([0-9]+)", ErrorMessage = "Please enter valid Postal Code")]
        [RegularExpression(@"^(?!00000)[0-9]{5,5}$", ErrorMessage = "Please enter valid Postal Code")]
        public string Zipcode { get; set; }
        public string Notes { get; set; }
        // public IList<CategoryModel> CategoriesModel { get; set; }

        [Display(Name = "Occasions")]
        public string Categories { get; set; }
    }
}
