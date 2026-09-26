using System.ComponentModel.DataAnnotations.Schema;

namespace ElectronicsShop.Models
{
    public partial class Product
    {
        [NotMapped]
        public bool IsLowStock
        {
            get { return Stock < 10; }
        }

        [NotMapped]
        public string TagsText
        {
            get
            {
                List<string> names = new List<string>();
                foreach (Tag tag in Tags.OrderBy(t => t.Name))
                {
                    names.Add("#" + tag.Name);
                }

                return string.Join(" ", names);
            }
        }
    }
}
