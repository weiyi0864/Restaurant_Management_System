using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using RestaurantManagementSystem.Models;

namespace RestaurantManagementSystem.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Reservation> Reservations { get; set; }
        public DbSet<MenuItem> MenuItems { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure relationships
            modelBuilder.Entity<Reservation>()
                .HasOne(r => r.User)
                .WithMany(u => u.Reservations)
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Order>()
                .HasOne(o => o.User)
                .WithMany(u => u.Orders)
                .HasForeignKey(o => o.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Order>()
                .HasOne(o => o.Reservation)
                .WithMany(r => r.Orders)
                .HasForeignKey(o => o.ReservationId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<OrderItem>()
                .HasOne(oi => oi.Order)
                .WithMany(o => o.OrderItems)
                .HasForeignKey(oi => oi.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<OrderItem>()
                .HasOne(oi => oi.MenuItem)
                .WithMany(mi => mi.OrderItems)
                .HasForeignKey(oi => oi.MenuItemId)
                .OnDelete(DeleteBehavior.Restrict);

            // Seed data for menu items
            modelBuilder.Entity<MenuItem>().HasData(
                new MenuItem { Id = 1, Name = "Margherita Pizza", Description = "Classic pizza with tomato sauce, mozzarella, and basil", Price = 12.99m, Category = "Pizza", ImageUrl = "/images/menu/margherita.jpg" },
                new MenuItem { Id = 2, Name = "Pepperoni Pizza", Description = "Pizza with tomato sauce, mozzarella, and pepperoni", Price = 14.99m, Category = "Pizza", ImageUrl = "/images/menu/pepperoni.jpg" },
                new MenuItem { Id = 3, Name = "Caesar Salad", Description = "Romaine lettuce, croutons, parmesan cheese with Caesar dressing", Price = 8.99m, Category = "Salad", ImageUrl = "/images/menu/caesar.jpg" },
                new MenuItem { Id = 4, Name = "Spaghetti Bolognese", Description = "Spaghetti pasta with rich meat sauce", Price = 15.99m, Category = "Pasta", ImageUrl = "/images/menu/bolognese.jpg" },
                new MenuItem { Id = 5, Name = "Chicken Alfredo", Description = "Fettuccine pasta with creamy alfredo sauce and grilled chicken", Price = 16.99m, Category = "Pasta", ImageUrl = "/images/menu/alfredo.jpg" },
                new MenuItem { Id = 6, Name = "Tiramisu", Description = "Classic Italian dessert with coffee-soaked ladyfingers and mascarpone cream", Price = 7.99m, Category = "Dessert", ImageUrl = "/images/menu/tiramisu.jpg" },
                new MenuItem { Id = 7, Name = "Cheesecake", Description = "New York style cheesecake with berry compote", Price = 8.99m, Category = "Dessert", ImageUrl = "/images/menu/cheesecake.jpg" },
                new MenuItem { Id = 8, Name = "Soft Drink", Description = "Assorted soft drinks (Coke, Sprite, etc.)", Price = 2.99m, Category = "Beverage", ImageUrl = "/images/menu/softdrink.jpg" },
                new MenuItem { Id = 9, Name = "Garlic Bread", Description = "Toasted bread with garlic butter and herbs", Price = 4.99m, Category = "Appetizer", ImageUrl = "/images/menu/garlicbread.jpg" },
                new MenuItem { Id = 10, Name = "Mozzarella Sticks", Description = "Breaded and fried mozzarella cheese sticks with marinara sauce", Price = 8.99m, Category = "Appetizer", ImageUrl = "/images/menu/mozzarellasticks.jpg" }
            );
        }
    }
}
