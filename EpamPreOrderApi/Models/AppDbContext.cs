using Microsoft.EntityFrameworkCore;

namespace EpamPreOrderApi.Models;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<GroupReservation> GroupReservations => Set<GroupReservation>();
    public DbSet<GuestSession> Sessions => Set<GuestSession>();
    public DbSet<Dish> Dishes => Set<Dish>();
    public DbSet<Order> Orders => Set<Order>();

    public void SeedData()
    {
        if (GroupReservations.Any()) return;

        var caesar = new Dish { Id = "dish1", Name = "Caesar Salad", Price = 180 };
        var tomyum = new Dish { Id = "dish2", Name = "Tom Yum", Price = 220 };

        var session1 = new GuestSession
        {
            Id = "session1",
            GroupReservationId = "group1",
            ReservationId = "reservation-123",
            Type = "Guest",
            Name = "Andrii",
            IsSubmitted = true,
            Items = new() { new OrderItem { Dish = caesar, DishId = caesar.Id, Quantity = 2 } }
        };

        var session2 = new GuestSession
        {
            Id = "session2",
            GroupReservationId = "group1",
            ReservationId = "reservation-123",
            Type = "Guest",
            Name = "Olena",
            IsSubmitted = false,
            Items = new() { new OrderItem { Dish = tomyum, DishId = tomyum.Id, Quantity = 1 } }
        };

        var ownerSession = new GuestSession
        {
            Id = "owner-session-123",
            GroupReservationId = "group1",
            ReservationId = "reservation-123",
            Type = "Owner",
            Name = "Owner User",
            IsSubmitted = true,
            Items = new() { new OrderItem { Dish = caesar, DishId = caesar.Id, Quantity = 1 } }
        };

        var order = new Order { Id = "order1", Status = "Pending" };

        var group = new GroupReservation
        {
            Id = "group1",
            ReservationId = "reservation-123",
            Status = "Active",
            Order = order,
            Sessions = new() { session1, session2, ownerSession }
        };

        GroupReservations.Add(group);
        SaveChanges();
    }
}