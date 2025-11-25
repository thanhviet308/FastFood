using FastFoodShop.Domain.Interfaces;
using FastFoodShop.Data;
using FastFoodShop.Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;


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

            var tracked = _db.Products.Local.FirstOrDefault(p => p.Id == product.Id);
            if (tracked != null)
            {
                _db.Entry(tracked).CurrentValues.SetValues(product);
            }
            else
            {
                _db.Products.Attach(product);
                _db.Entry(product).State = EntityState.Modified;
            }

            var result = await _db.SaveChangesAsync();

            return result;
        }


        // ------------------- Product -------------------

        public async Task<Product> CreateAsync(Product pr)
        {
            _db.Products.Add(pr);
            await _db.SaveChangesAsync();
            return pr;
        }

        public async Task<(IReadOnlyList<Product> Items, int Total)> FetchAllAsync(int page, int size)
        {
            page = Math.Max(1, page);
            size = Math.Clamp(size, 1, 100);

            var query = _db.Products.AsNoTracking()
                .Include(p => p.Category)
                .OrderBy(p => p.Id);
            var total = await query.CountAsync();
            var items = await query.Skip((page - 1) * size).Take(size).ToListAsync();
            return (items, total);
        }

        public async Task<(IReadOnlyList<Product> Items, int Total)> FetchAsync(int page, int size)
        {
            page = Math.Max(1, page);
            size = Math.Clamp(size, 1, 100);

            var query = _db.Products.AsNoTracking()
                .Where(p => p.IsActive)
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
            var query = _db.Products.AsNoTracking().Where(p => p.IsFeatured && p.IsActive);
            var total = await query.CountAsync();


            var items = await query
                .OrderByDescending(p => p.UpdatedAt ?? p.CreatedAt ?? DateTime.UtcNow)
                .ThenByDescending(p => p.Id)
                .Skip((page - 1) * size)
                .Take(size)
                .ToListAsync();



            return (items, total);
        }

        public async Task<(IReadOnlyList<Product> Items, int Total)> FetchBaseAsync(int page, int size, long? categoryId = null)
        {
            page = Math.Max(1, page);
            size = Math.Clamp(size, 1, 100);

            var baseQuery = _db.Products.AsNoTracking()
                .Where(p => p.IsActive);
            if (categoryId.HasValue)
            {
                baseQuery = baseQuery.Where(p => p.CategoryId == categoryId.Value);
            }
            
            // Get all products without grouping to show all variants
            var allProducts = await baseQuery.ToListAsync();
                
            var total = allProducts.Count;
            var items = allProducts
                .OrderByDescending(p => p.Id)
                .Skip((page - 1) * size)
                .Take(size)
                .ToList();
                
            return (items, total);
        }

        public async Task<IReadOnlyList<Product>> FetchAllAsync()
        {
            var baseQuery = _db.Products.AsNoTracking()
                .Where(p => p.IsActive);
            var allProducts = await baseQuery.ToListAsync();
            var items = allProducts
                .OrderByDescending(p => p.Id)
                .ToList();
            return items;
        }

        public async Task<(IReadOnlyList<Product> Items, int Total)> FetchWithSpecAsync(
            int page, int size, ProductCriteriaDto criteria)
        {
            // Hiện tại schema mới không lọc theo factory/target/price ở Product
            return await FetchBaseAsync(page, size);
        }

        public Task<Product?> GetByIdAsync(long id)
            => _db.Products.AsTracking()
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.Id == id);

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
            if (string.IsNullOrWhiteSpace(email))
            {
                return 0;
            }

            if (quantity <= 0) quantity = 1;
            if (quantity > 999) quantity = 999;

            var user = await _userService.GetUserByEmailAsync(email);
            if (user is null)
            {
                return 0;
            }

            await using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                // Lấy hoặc tạo giỏ hàng (kèm details)
                var cart = await _db.Carts
                    .Include(c => c.CartDetails)
                    .FirstOrDefaultAsync(c => c.UserId == user.Id);

                if (cart is null)
                {
                    cart = new Cart
                    {
                        UserId = user.Id,
                        Sum = 0,
                        CartDetails = new List<CartDetail>()
                    };
                    _db.Carts.Add(cart);
                    await _db.SaveChangesAsync(); // cần cart.Id trước khi thêm detail
                }

                // Lấy sản phẩm
                var product = await _db.Products.FindAsync(productId);
                if (product is null)
                {
                    await tx.RollbackAsync();
                    return 0;
                }

                ProductVariant? variant = null;
                if (variantId.HasValue)
                {
                    variant = await _db.ProductVariants.FirstOrDefaultAsync(v => v.Id == variantId && v.ProductId == product.Id);
                }

                // Thêm / cập nhật dòng chi tiết
                if (variant is null)
                {
                    variant = await _db.ProductVariants.Where(v => v.ProductId == product.Id && v.IsActive)
                        .OrderBy(v => v.Price).FirstOrDefaultAsync();
                    if (variant is null)
                    {
                        await tx.RollbackAsync();
                        return 0;
                    }
                }

                var line = cart.CartDetails?.FirstOrDefault(x => x.ProductId == product.Id && x.VariantId == variant.Id);

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
                }
                else
                {
                    var newQty = line.Quantity + quantity;
                    line.Quantity = newQty > 999 ? 999 : newQty;
                    _db.CartDetails.Update(line);
                }

                // Tính lại tổng số lượng trong giỏ
                var totalQty = cart.CartDetails?.Sum(x => (long)x.Quantity) ?? 0;
                cart.Sum = totalQty > int.MaxValue ? int.MaxValue : (int)totalQty;
                _db.Carts.Update(cart);

                await _db.SaveChangesAsync();
                await tx.CommitAsync();

                // Count distinct products directly by CartId (fast & accurate)
                var distinct = await _db.CartDetails
                    .Where(d => d.CartId == cart.Id)
                    .Select(d => d.ProductId)
                    .Distinct()
                    .CountAsync();

                // Optional: set session if used elsewhere
                session?.SetInt32("sum", cart.Sum);
                session?.SetInt32("distinct", distinct);

                return distinct; // Return distinct count for FE badge update
            }
            catch (Exception ex)
            {
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
            var cart = await _db.Carts
                .Include(c => c.CartDetails!)
                    .ThenInclude(d => d.Product)
                .Include(c => c.CartDetails!)
                    .ThenInclude(d => d.Variant)
                .FirstOrDefaultAsync(c => c.UserId == user.Id);
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

        public async Task<long> HandlePlaceOrderAsync(
            User user, ISession session,
            string receiverName, string receiverAddress, string receiverPhone, string? note = null)
        {
            var cart = await _db.Carts
                .Include(c => c.CartDetails!)
                    .ThenInclude(d => d.Product)
                .Include(c => c.CartDetails!)
                    .ThenInclude(d => d.Variant)
                .FirstOrDefaultAsync(c => c.UserId == user.Id);

            if (cart is null || cart.CartDetails?.Count == 0)
            {
                return 0;
            }

            decimal sum = 0;
            foreach (var d in cart.CartDetails!)
            {
                var itemTotal = d.Price * d.Quantity;
                sum += itemTotal;
            }

            var order = new Order
            {
                UserId = user.Id,
                ReceiverName = receiverName,
                ReceiverAddress = receiverAddress,
                ReceiverPhone = receiverPhone,
                Status = "PENDING",
                PaymentStatus = "UNPAID", // Mặc định chưa thanh toán
                CreatedAt = DateTime.Now,
                TotalPrice = sum,
                Note = note
            };
            _db.Orders.Add(order);
            await _db.SaveChangesAsync();

            foreach (var d in cart.CartDetails)
            {
                var od = new OrderDetail
                {
                    OrderId = order.Id,
                    ProductId = d.ProductId,
                    Price = d.Price,
                    Quantity = d.Quantity,
                    VariantId = d.VariantId
                };
                _db.OrderDetails.Add(od);
            }
            _db.CartDetails.RemoveRange(cart.CartDetails);
            _db.Carts.Remove(cart);

            await _db.SaveChangesAsync();

            session.SetInt32("sum", 0);
            return order.Id;
        }

        async Task<int> IProductService.HandleAddProductToCartAsync(string email, long productId, ISession session, int quantity, long? variantId)
        {
            return await HandleAddProductToCartAsync(email, productId, session, quantity, variantId);
        }

        // Cập nhật số lượng của một CartDetail và trả về tổng tiền mới của giỏ hàng
        public async Task<decimal> UpdateCartQuantityAsync(long cartDetailId, int quantity, long userId, ISession session)
        {
            if (quantity < 1) quantity = 1;
            if (quantity > 999) quantity = 999;

            // Lấy cart detail kèm cart + variant để tính giá
            var detail = await _db.CartDetails
                .Include(d => d.Cart!)
                    .ThenInclude(c => c.CartDetails!)
                .FirstOrDefaultAsync(d => d.Id == cartDetailId);

            if (detail is null)
                throw new InvalidOperationException("Cart detail không tồn tại");

            if (detail.Cart?.UserId != userId)
                throw new InvalidOperationException("Không có quyền cập nhật mục này");

            detail.Quantity = quantity;
            _db.CartDetails.Update(detail);

            // Cập nhật lại tổng số lượng (Sum) và tính tổng tiền
            var cart = detail.Cart!;
            cart.Sum = cart.CartDetails?.Sum(cd => (int)cd.Quantity) ?? 0;
            _db.Carts.Update(cart);

            await _db.SaveChangesAsync();

            // Tính tổng tiền mới
            decimal newTotal = 0m;
            foreach (var cd in cart.CartDetails!)
            {
                newTotal += cd.Price * cd.Quantity;
            }

            // Distinct count cho badge
            var distinct = cart.CartDetails
                .Select(cd => cd.ProductId)
                .Distinct()
                .Count();

            session?.SetInt32("sum", cart.Sum);
            session?.SetInt32("distinct", distinct);

            return newTotal;
        }

        // ------------------- Session Cart (for anonymous users) -------------------

        public async Task<int> HandleAddProductToCartSessionAsync(long productId, ISession session, int quantity, long? variantId)
        {
            if (quantity <= 0) quantity = 1;
            if (quantity > 999) quantity = 999;

            // Lấy giỏ hàng từ Session
            var cartJson = session.GetString("AnonymousCart");
            var cartItems = string.IsNullOrEmpty(cartJson)
                ? new List<CartItemSession>()
                : System.Text.Json.JsonSerializer.Deserialize<List<CartItemSession>>(cartJson) ?? new List<CartItemSession>();

            // Lấy sản phẩm
            var product = await _db.Products.FindAsync(productId);
            if (product is null) return cartItems.Count;

            // Lấy variant
            ProductVariant? variant = null;
            if (variantId.HasValue)
            {
                variant = await _db.ProductVariants.FirstOrDefaultAsync(v => v.Id == variantId && v.ProductId == product.Id);
            }

            if (variant is null)
            {
                variant = await _db.ProductVariants.Where(v => v.ProductId == product.Id && v.IsActive)
                    .OrderBy(v => v.Price).FirstOrDefaultAsync();
                if (variant is null) return cartItems.Count;
            }

            // Tìm item trong giỏ hàng
            var existingItem = cartItems.FirstOrDefault(x => x.ProductId == product.Id && x.VariantId == variant.Id);

            if (existingItem != null)
            {
                existingItem.Quantity = Math.Min(existingItem.Quantity + quantity, 999);
            }
            else
            {
                cartItems.Add(new CartItemSession
                {
                    ProductId = product.Id,
                    VariantId = variant.Id,
                    Price = variant.Price,
                    Quantity = quantity
                });
            }

            // Lưu lại vào Session
            var updatedJson = System.Text.Json.JsonSerializer.Serialize(cartItems);
            session.SetString("AnonymousCart", updatedJson);
            var distinct = cartItems.Count;
            var sum = cartItems.Sum(x => x.Quantity);
            session.SetInt32("distinct", distinct);
            session.SetInt32("sum", sum);

            return distinct;
        }

        public List<CartItemSession> GetCartFromSession(ISession session)
        {
            var cartJson = session.GetString("AnonymousCart");
            return string.IsNullOrEmpty(cartJson)
                ? new List<CartItemSession>()
                : System.Text.Json.JsonSerializer.Deserialize<List<CartItemSession>>(cartJson) ?? new List<CartItemSession>();
        }

        public async Task<long> HandlePlaceOrderFromSessionAsync(
            ISession session,
            string receiverName, string receiverAddress, string receiverPhone, string? note = null)
        {
            var cartItems = GetCartFromSession(session);
            if (cartItems.Count == 0) return 0;

            decimal sum = 0;
            foreach (var item in cartItems)
            {
                var variant = await _db.ProductVariants
                    .Include(v => v.Product)
                    .FirstOrDefaultAsync(v => v.Id == item.VariantId);
                if (variant != null)
                {
                    sum += item.Price * item.Quantity;
                }
            }

            var order = new Order
            {
                UserId = null, // Anonymous order
                ReceiverName = receiverName,
                ReceiverAddress = receiverAddress,
                ReceiverPhone = receiverPhone,
                Status = "PENDING",
                PaymentStatus = "UNPAID", // Mặc định chưa thanh toán
                CreatedAt = DateTime.Now,
                TotalPrice = sum,
                Note = note
            };
            _db.Orders.Add(order);
            await _db.SaveChangesAsync();

            foreach (var item in cartItems)
            {
                var variant = await _db.ProductVariants.FirstOrDefaultAsync(v => v.Id == item.VariantId);
                if (variant != null)
                {
                    var od = new OrderDetail
                    {
                        OrderId = order.Id,
                        ProductId = item.ProductId,
                        Price = item.Price,
                        Quantity = item.Quantity,
                        VariantId = item.VariantId
                    };
                    _db.OrderDetails.Add(od);
                }
            }

            await _db.SaveChangesAsync();
            return order.Id;

            // Xóa giỏ hàng trong Session
            session.Remove("AnonymousCart");
            session.SetInt32("sum", 0);
            session.SetInt32("distinct", 0);
        }

        public async Task<ProductVariant?> GetVariantByIdAsync(long variantId)
        {
            return await _db.ProductVariants
                .Include(v => v.Product)
                .FirstOrDefaultAsync(v => v.Id == variantId);
        }

        public void RemoveCartItemFromSession(ISession session, long productId, long variantId)
        {
            var cartItems = GetCartFromSession(session);
            cartItems.RemoveAll(x => x.ProductId == productId && x.VariantId == variantId);
            var updatedJson = System.Text.Json.JsonSerializer.Serialize(cartItems);
            session.SetString("AnonymousCart", updatedJson);
            var distinct = cartItems.Count;
            var sum = cartItems.Sum(x => x.Quantity);
            session.SetInt32("distinct", distinct);
            session.SetInt32("sum", sum);
        }

    }
}
