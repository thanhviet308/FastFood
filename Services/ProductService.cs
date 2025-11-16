using FastFoodShop.Domain.Interfaces;
using FastFoodShop.Data;
using FastFoodShop.Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace FastFoodShop.Services
{

    public class ProductService : IProductService
    {
        private readonly AppDbContext _db;
        private readonly IUserService _userService;

        public ProductService(AppDbContext db, IUserService userService)
        {
            _db = db;
            _userService = userService;
        }

        public async Task<int> UpdateAsync(Product product)
        {
            Console.WriteLine($"=== PRODUCT SERVICE UPDATE DEBUG ===");
            Console.WriteLine($"UpdateAsync called for Product ID: {product.Id}");
            Console.WriteLine($"Product Name: {product.Name}");
            Console.WriteLine($"Product Image: {product.Image}");
            
            var tracked = _db.Products.Local.FirstOrDefault(p => p.Id == product.Id);
            if (tracked != null)
            {
                Console.WriteLine($"Found tracked entity, updating CurrentValues");
                Console.WriteLine($"Before update - Tracked Image: {tracked.Image}");
                _db.Entry(tracked).CurrentValues.SetValues(product);
                Console.WriteLine($"After CurrentValues.SetValues - Tracked Image: {tracked.Image}");
            }
            else
            {
                Console.WriteLine($"No tracked entity found, attaching and setting Modified");
                _db.Products.Attach(product);
                _db.Entry(product).State = EntityState.Modified;
                Console.WriteLine($"Entity attached and set to Modified state");
            }
            
            Console.WriteLine($"About to call SaveChangesAsync...");
            var result = await _db.SaveChangesAsync();
            Console.WriteLine($"SaveChangesAsync returned: {result}");
            
            // Check the entity state after save
            var entity = tracked ?? product;
            Console.WriteLine($"After save - Entity Image: {entity.Image}");
            Console.WriteLine($"Entity State: {_db.Entry(entity).State}");
            
            return result;
        }


        // ------------------- Product -------------------

        public async Task<Product> CreateAsync(Product pr)
        {
            _db.Products.Add(pr);
            await _db.SaveChangesAsync();
            return pr;
        }

        public async Task<(IReadOnlyList<Product> Items, int Total)> FetchAsync(int page, int size)
        {
            page = Math.Max(1, page);
            size = Math.Clamp(size, 1, 100);

            var query = _db.Products.AsNoTracking()
                .Include(p => p.Category)
                .OrderByDescending(p => p.Id);
            var total = await query.CountAsync();
            var items = await query.Skip((page - 1) * size).Take(size).ToListAsync();
            return (items, total);
        }

        public async Task<(IReadOnlyList<Product> Items, int Total)> FetchFeaturedAsync(int page, int size)
        {
            page = Math.Max(1, page);
            size = Math.Max(1, size);
            var query = _db.Products.AsNoTracking().Where(p => p.IsFeatured);
            var total = await query.CountAsync();
            Console.WriteLine($"FetchFeaturedAsync - Total featured products: {total}");
            
            var items = await query
                .OrderByDescending(p => p.UpdatedAt ?? p.CreatedAt ?? DateTime.UtcNow)
                .ThenByDescending(p => p.Id)
                .Skip((page - 1) * size)
                .Take(size)
                .ToListAsync();
                
            Console.WriteLine($"FetchFeaturedAsync - Loaded {items.Count} products");
            foreach (var product in items)
            {
                Console.WriteLine($"Product: ID={product.Id}, Name={product.Name}, Featured={product.IsFeatured}");
            }
            
            return (items, total);
        }

        public async Task<(IReadOnlyList<Product> Items, int Total)> FetchBaseAsync(int page, int size)
        {
            page = Math.Max(1, page);
            size = Math.Clamp(size, 1, 100);

            var baseQuery = _db.Products.AsNoTracking().Where(p => p.IsActive);
            var total = await baseQuery.GroupBy(p => p.Name).CountAsync();
            var items = await baseQuery
                .GroupBy(p => p.Name)
                .Select(g => g.OrderByDescending(p => p.Id).First())
                .OrderByDescending(p => p.Id)
                .Skip((page - 1) * size)
                .Take(size)
                .ToListAsync();
            return (items, total);
        }

        public async Task<(IReadOnlyList<Product> Items, int Total)> FetchWithSpecAsync(
            int page, int size, ProductCriteriaDto criteria)
        {
            // Hiện tại schema mới không lọc theo factory/target/price ở Product
            return await FetchBaseAsync(page, size);
        }

        public Task<Product?> GetByIdAsync(long id)
            => _db.Products.AsTracking().FirstOrDefaultAsync(p => p.Id == id);

        public async Task DeleteAsync(long id)
        {
            var entity = await _db.Products.FindAsync(id);
            if (entity is null) return;
            _db.Products.Remove(entity);
            await _db.SaveChangesAsync();
        }

        // ------------------- Cart -------------------

        public async Task<int> HandleAddProductToCartAsync(string email, long productId, ISession session, int quantity, long? variantId)
        {
            Console.WriteLine($"HandleAddProductToCartAsync called - Email: {email}, ProductId: {productId}, Quantity: {quantity}, VariantId: {variantId}");
            
            if (string.IsNullOrWhiteSpace(email)) 
            {
                Console.WriteLine("Email is null or empty, returning 0");
                return 0;
            }
            
            if (quantity <= 0) quantity = 1;
            if (quantity > 999) quantity = 999;

            Console.WriteLine($"Looking up user by email: {email}");
            var user = await _userService.GetUserByEmailAsync(email);
            if (user is null) 
            {
                Console.WriteLine($"User not found for email: {email}");
                return 0;
            }
            
            Console.WriteLine($"User found - ID: {user.Id}, Email: {user.Email}");

            await using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                // Lấy hoặc tạo giỏ hàng (kèm details)
                Console.WriteLine($"Getting or creating cart for User ID: {user.Id}");
                var cart = await _db.Carts
                    .Include(c => c.CartDetails)
                    .FirstOrDefaultAsync(c => c.UserId == user.Id);

                if (cart is null)
                {
                    Console.WriteLine($"No existing cart found, creating new cart for User ID: {user.Id}");
                    cart = new Cart
                    {
                        UserId = user.Id,
                        Sum = 0,
                        CartDetails = new List<CartDetail>()
                    };
                    _db.Carts.Add(cart);
                    await _db.SaveChangesAsync(); // cần cart.Id trước khi thêm detail
                    Console.WriteLine($"New cart created with ID: {cart.Id}");
                }
                else
                {
                    Console.WriteLine($"Existing cart found for User ID: {user.Id}, Cart ID: {cart.Id}, CartDetails count: {cart.CartDetails?.Count ?? 0}");
                }

                // Lấy sản phẩm
                var product = await _db.Products.FindAsync(productId);
                if (product is null)
                {
                    Console.WriteLine($"Product not found with ID: {productId}");
                    await tx.RollbackAsync();
                    return 0;
                }
                Console.WriteLine($"Product found: ID={product.Id}, Name={product.Name}");

                ProductVariant? variant = null;
                if (variantId.HasValue)
                {
                    Console.WriteLine($"Looking for specific variant ID: {variantId}");
                    variant = await _db.ProductVariants.FirstOrDefaultAsync(v => v.Id == variantId && v.ProductId == product.Id);
                }

                // Thêm / cập nhật dòng chi tiết
                if (variant is null)
                {
                    Console.WriteLine($"No specific variant provided, looking for default variant for product ID: {product.Id}");
                    variant = await _db.ProductVariants.Where(v => v.ProductId == product.Id && v.IsActive)
                        .OrderBy(v => v.Price).FirstOrDefaultAsync();
                    if (variant is null)
                    {
                        Console.WriteLine($"No active variant found for product ID: {product.Id}");
                        await tx.RollbackAsync();
                        return 0;
                    }
                    Console.WriteLine($"Found default variant: ID={variant.Id}, Price={variant.Price}");
                }

                var line = cart.CartDetails?.FirstOrDefault(x => x.ProductId == product.Id && x.VariantId == variant.Id);
                Console.WriteLine($"Existing cart detail found: {line != null}");
                
                if (line is null)
                {
                    line = new CartDetail
                    {
                        CartId = cart.Id,
                        ProductId = product.Id,
                        Price = variant.Price,
                        VariantId = variant.Id,
                        Quantity = quantity
                    };
                    _db.CartDetails.Add(line);
                    cart.CartDetails!.Add(line);
                    Console.WriteLine($"New cart detail created for Product ID: {product.Id}, Variant ID: {variant.Id}, Quantity: {quantity}");
                }
                else
                {
                    var newQty = line.Quantity + quantity;
                    line.Quantity = newQty > 999 ? 999 : newQty;
                    _db.CartDetails.Update(line);
                    Console.WriteLine($"Updated cart detail ID: {line.Id}, new quantity: {line.Quantity}");
                }

                // Tính lại tổng số lượng trong giỏ
                var totalQty = cart.CartDetails?.Sum(x => (long)x.Quantity) ?? 0;
                cart.Sum = totalQty > int.MaxValue ? int.MaxValue : (int)totalQty;
                _db.Carts.Update(cart);

                Console.WriteLine("About to save changes to database...");
                await _db.SaveChangesAsync();
                Console.WriteLine("Database changes saved successfully");
                await tx.CommitAsync();
                Console.WriteLine("Transaction committed successfully");

                // ✅ Đếm DISTINCT trực tiếp theo CartId (nhanh & chính xác)
                var distinct = await _db.CartDetails
                    .Where(d => d.CartId == cart.Id)
                    .Select(d => d.ProductId)
                    .Distinct()
                    .CountAsync();

                Console.WriteLine($"Cart operation completed. Distinct items: {distinct}, Total quantity: {cart.Sum}");

                // Optional: set session nếu nơi khác dùng
                session?.SetInt32("sum", cart.Sum);
                session?.SetInt32("distinct", distinct);

                return distinct; // 👈 Trả về số loại khác nhau để FE cập nhật badge
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR in HandleAddProductToCartAsync: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                await tx.RollbackAsync();
                throw;
            }
        }


        public async Task<int> GetDistinctCountByEmailAsync(string email)
        {
            var user = await _userService.GetUserByEmailAsync(email);
            if (user is null) return 0;

            return await _db.CartDetails
                .Where(d => d.Cart!.UserId == user.Id)
                .Select(d => d.ProductId)
                .Distinct()
                .CountAsync();
        }

        public async Task<IReadOnlyList<Category>> GetAllCategoriesAsync()
            => await _db.Categories.AsNoTracking().OrderBy(c => c.Name).ToListAsync();

        public Task<Category?> GetCategoryByIdAsync(long id)
            => _db.Categories.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);

        public async Task<Category> CreateCategoryAsync(Category category)
        {
            _db.Categories.Add(category);
            await _db.SaveChangesAsync();
            return category;
        }

        public async Task UpdateCategoryAsync(Category category)
        {
            var exist = await _db.Categories.FindAsync(category.Id);
            if (exist is null) return;
            exist.Name = category.Name;
            exist.Description = category.Description;
            exist.IsActive = category.IsActive;
            _db.Categories.Update(exist);
            await _db.SaveChangesAsync();
        }

        public async Task DeleteCategoryAsync(long id)
        {
            var exist = await _db.Categories.FindAsync(id);
            if (exist is null) return;
            _db.Categories.Remove(exist);
            await _db.SaveChangesAsync();
        }

        public async Task<IReadOnlyList<ProductVariant>> GetVariantsAsync(long productId)
            => await _db.ProductVariants.AsNoTracking()
                .Where(v => v.ProductId == productId)
                .OrderBy(v => v.VariantName)
                .ToListAsync();

        public async Task<ProductVariant> AddVariantAsync(long productId, string variantName, decimal price)
        {
            var name = (variantName ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("variantName required");
            if (price <= 0) throw new ArgumentException("price must be > 0");

            var exist = await _db.ProductVariants.FirstOrDefaultAsync(v => v.ProductId == productId && v.VariantName == name);
            if (exist != null)
            {
                // nếu đã tồn tại, cập nhật giá và bật active
                exist.Price = price;
                exist.IsActive = true;
                _db.ProductVariants.Update(exist);
                await _db.SaveChangesAsync();
                return exist;
            }

            var v = new ProductVariant { ProductId = productId, VariantName = name, Price = price, IsActive = true };
            _db.ProductVariants.Add(v);
            await _db.SaveChangesAsync();
            return v;
        }

        public async Task UpdateVariantAsync(ProductVariant variant)
        {
            var exist = await _db.ProductVariants.FindAsync(variant.Id);
            if (exist == null) return;
            exist.VariantName = variant.VariantName;
            exist.Price = variant.Price;
            exist.IsActive = variant.IsActive;
            _db.ProductVariants.Update(exist);
            await _db.SaveChangesAsync();
        }

        public async Task DeleteVariantAsync(long variantId)
        {
            var exist = await _db.ProductVariants.FindAsync(variantId);
            if (exist == null) return;
            _db.ProductVariants.Remove(exist);
            await _db.SaveChangesAsync();
        }

        public async Task<Cart?> GetCartByUserAsync(User user)
        {
            Console.WriteLine($"GetCartByUserAsync called for User ID: {user.Id}");
            var cart = await _db.Carts.Include(c => c.CartDetails!).ThenInclude(d => d.Product)
                              .FirstOrDefaultAsync(c => c.UserId == user.Id);
            Console.WriteLine($"Cart found: {cart != null}, CartDetails count: {cart?.CartDetails?.Count ?? 0}");
            return cart;
        }

        public async Task HandleRemoveCartDetailAsync(long cartDetailId, ISession session)
        {
            var cd = await _db.CartDetails.Include(x => x.Cart)
                                          .FirstOrDefaultAsync(x => x.Id == cartDetailId);
            if (cd is null) return;

            var currentCart = cd.Cart!;
            _db.CartDetails.Remove(cd);
            await _db.SaveChangesAsync();

            if (currentCart.Sum > 1)
            {
                currentCart.Sum = currentCart.Sum - 1;
                _db.Carts.Update(currentCart);
                await _db.SaveChangesAsync();
                session.SetInt32("sum", currentCart.Sum);
            }
            else
            {
                _db.Carts.Remove(currentCart);
                await _db.SaveChangesAsync();
                session.SetInt32("sum", 0);
            }
        }

        public async Task HandleUpdateCartBeforeCheckoutAsync(List<CartDetail> cartDetails)
        {
            foreach (var item in cartDetails)
            {
                var exist = await _db.CartDetails.FindAsync(item.Id);
                if (exist is null) continue;
                exist.Quantity = item.Quantity;
                _db.CartDetails.Update(exist);
            }
            await _db.SaveChangesAsync();
        }

        // ------------------- Order -------------------

        public async Task HandlePlaceOrderAsync(
            User user, ISession session,
            string receiverName, string receiverAddress, string receiverPhone)
        {
            Console.WriteLine($"HandlePlaceOrderAsync called for user: {user.Id}, receiver: {receiverName}");
            
            var cart = await _db.Carts.Include(c => c.CartDetails!).ThenInclude(d => d.Product)
                                      .FirstOrDefaultAsync(c => c.UserId == user.Id);

            if (cart is null || cart.CartDetails?.Count == 0) 
            {
                Console.WriteLine("Cart is empty or null, returning");
                return;
            }

            Console.WriteLine($"Cart found with {cart.CartDetails?.Count ?? 0} items");

            decimal sum = 0;
            foreach (var d in cart.CartDetails!)
            {
                var itemTotal = d.Price * d.Quantity;
                sum += itemTotal;
                Console.WriteLine($"Item: {d.ProductId}, Price: {d.Price}, Quantity: {d.Quantity}, Total: {itemTotal}");
            }
            
            Console.WriteLine($"Total order amount: {sum}");

            var order = new Order
            {
                UserId = user.Id,
                ReceiverName = receiverName,
                ReceiverAddress = receiverAddress,
                ReceiverPhone = receiverPhone,
                Status = "PENDING",
                TotalPrice = sum
            };
            
            Console.WriteLine("Creating order...");
            _db.Orders.Add(order);
            await _db.SaveChangesAsync();
            Console.WriteLine($"Order created with ID: {order.Id}");

            Console.WriteLine("Creating order details...");
            foreach (var d in cart.CartDetails)
            {
                var od = new OrderDetail
                {
                    OrderId = order.Id,
                    ProductId = d.ProductId,
                    Price = d.Price,
                    Quantity = d.Quantity
                };
                _db.OrderDetails.Add(od);
                Console.WriteLine($"Order detail: Product {d.ProductId}, Price {d.Price}, Quantity {d.Quantity}");
            }

            Console.WriteLine("Removing cart items...");
            _db.CartDetails.RemoveRange(cart.CartDetails);
            _db.Carts.Remove(cart);

            await _db.SaveChangesAsync();
            Console.WriteLine("Order placed successfully");

            session.SetInt32("sum", 0);
        }

        async Task<int> IProductService.HandleAddProductToCartAsync(string email, long productId, ISession session, int quantity, long? variantId)
        {
            return await HandleAddProductToCartAsync(email, productId, session, quantity, variantId);
        }
    }

    internal static class Predicate
    {
        public static Expression<Func<T, bool>> True<T>() => _ => true;
        public static Expression<Func<T, bool>> False<T>() => _ => false;

        public static Expression<Func<T, bool>> Or<T>(
            this Expression<Func<T, bool>> expr1,
            Expression<Func<T, bool>> expr2)
        {
            var invoked = Expression.Invoke(expr2, expr1.Parameters);
            return Expression.Lambda<Func<T, bool>>(
                Expression.OrElse(expr1.Body, invoked), expr1.Parameters);
        }
    }
}
