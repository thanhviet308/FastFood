using FastFoodShop.Domain.Entities;
using System.Linq.Expressions;

namespace FastFoodShop.Domain.Specifications
{
    public static class ProductSpecifications
    {
        public static Expression<Func<Product, bool>> NameLike(string name)
            => p => p.Name.Contains(name);
    }
}
