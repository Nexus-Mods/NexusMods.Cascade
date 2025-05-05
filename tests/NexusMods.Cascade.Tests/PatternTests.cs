using System.Data;
using FluentAssertions;
using NexusMods.Cascade.Patterns;

namespace NexusMods.Cascade.Tests;

public class PatternTests
{

    [Fact]
    public void CanRunASimpleJoinRule()
    {
        var distances = new Inlet<(string CityName, int Distance)>();
        var friends = new Inlet<(string Name, string CityName)>();

        var flow = Patterns.Pattern.Create()
            .Match(distances, out var city, out var distance)
            .Project(distance, d => d * 4, out var distance4)
            .Match(friends, out var name, city)
            .Return(name, distance4.Max(), distance.Count(), distance.Sum());


        var t = new Topology();

        var distancesInlet = t.Intern(distances);
        var friendsInlet = t.Intern(friends);

        using var results = t.Query(flow);

        distancesInlet.Values = new[]
        {
            ("Seattle", 0),
            ("Portland", 3),
            ("San Francisco", 7),
        };

        friendsInlet.Values = new[]
        {
            ("Alice", "Seattle"),
            ("Alice", "San Francisco"),
            ("Bob", "Portland"),
            ("Charlie", "San Francisco"),
        };

        results.Should().BeEquivalentTo(
            new[]
            {
                ("Alice", 28, 2),
                ("Bob", 12, 1),
                ("Charlie", 28, 1),
            }, o => o.WithoutStrictOrdering());

    }

    [Fact]
    public async Task CanJoinTransactionsAndAccounts()
    {
        // Arrange
        // Inlet holding account information.
        var accounts = new Inlet<(int AccountId, string AccountName)>();
        // Inlet holding transaction records.
        var transactions = new Inlet<(int AccountId, decimal Amount)>();

        // Create a flow that:
        // 1. Matches transactions and extracts the AccountId and Amount.
        // 2. Joins with the accounts inlet on the AccountId.
        // 3. Aggregates the Amount values (summing them per account).
        var flow = Pattern.Create()
            .Match(transactions, out var accId, out var amount)
            .Match(accounts, accId, out var accountName)
            .Return(accountName, amount.Sum());

        var topology = new Topology();
        var accountsNode = topology.Intern(accounts);
        var transactionsNode = topology.Intern(transactions);
        using var results = topology.Query(flow);

        // Act
        accountsNode.Values = new[]
        {
            (1, "Alice"),
            (2, "Bob")
        };

        transactionsNode.Values = new[]
        {
            (1, 100.0m),
            (1, 150.0m),
            (2, 200.0m),
        };

        await topology.FlushEffectsAsync();

        // Assert
        results.Should().BeEquivalentTo(
            new[]
            {
                ("Alice", 250.0m),
                ("Bob", 200.0m)
            },
            options => options.WithoutStrictOrdering());
    }

    [Fact]
    public async Task CanJoinAndProjectCityStats()
    {
        // Arrange
        // Inlet with city statistics
        var cities = new Inlet<(string City, int Population)>();
        // Inlet with persons and their associated city.
        var persons = new Inlet<(string Name, string City)>();

        // Create a flow that:
        // 1. Matches city stats and extracts City and Population.
        // 2. Projects the City name into upper-case.
        // 3. Joins the persons inlet based on the original City name.
        // 4. Returns the person name, the projected (upper-case) city name,
        //    and a population aggregate (here we use Max, although for a single match it returns Population).
        var flow = Pattern.Create()
            .Match(cities, out var city, out var population)
            .Project(city, c => c.ToUpperInvariant(), out var cityUpper)
            .Match(persons, out var personName, city)
            .Return(personName, cityUpper, population.Max());

        var topology = new Topology();
        var citiesNode = topology.Intern(cities);
        var personsNode = topology.Intern(persons);
        using var results = topology.Query(flow);

        // Act
        citiesNode.Values = new[]
        {
            ("Seattle", 700000),
            ("Portland", 650000)
        };

        personsNode.Values = new[]
        {
            ("Alice", "Seattle"),
            ("Bob", "Portland"),
            ("Charlie", "Seattle"),
        };

        await topology.FlushEffectsAsync();

        // Assert
        results.Should().BeEquivalentTo(
            new[]
            {
                ("Alice", "SEATTLE", 700000),
                ("Bob", "PORTLAND", 650000),
                ("Charlie", "SEATTLE", 700000)
            },
            options => options.WithoutStrictOrdering());
    }

    [Fact]
    public async Task CanJoinAcrossThreeSources()
    {
        // Arrange
        // Inlet for employees with a department ID.
        var employees = new Inlet<(int EmpId, string EmpName, int DeptId)>();
        // Inlet for departments with the department name and a location ID.
        var departments = new Inlet<(int DeptId, string DeptName, int LocationId)>();
        // Inlet for locations providing a location name.
        var locations = new Inlet<(int LocationId, string LocationName)>();

        // Create a flow that:
        // 1. Matches employees.
        // 2. Joins departments based on the department ID.
        // 3. Joins locations based on the location ID from the department.
        // 4. Returns a tuple with the employee name, department name, and location name.
        var flow = Pattern.Create()
            .Match(employees, out var empId, out var empName, out var deptId)
            .Match(departments, deptId, out var deptName, out var locationId)
            .Match(locations, locationId, out var locationName)
            .Return(empName, deptName, locationName);

        var topology = new Topology();
        var employeesNode = topology.Intern(employees);
        var departmentsNode = topology.Intern(departments);
        var locationsNode = topology.Intern(locations);
        using var results = topology.Query(flow);

        // Act
        employeesNode.Values = new[]
        {
            (1, "Alice", 10),
            (2, "Bob", 20),
            (3, "Charlie", 10),
        };

        departmentsNode.Values = new[]
        {
            (10, "Engineering", 100),
            (20, "Marketing", 200),
        };

        locationsNode.Values = new[]
        {
            (100, "New York"),
            (200, "Los Angeles"),
        };

        await topology.FlushEffectsAsync();

        // Assert
        results.Should().BeEquivalentTo(
            new[]
            {
                ("Alice", "Engineering", "New York"),
                ("Bob", "Marketing", "Los Angeles"),
                ("Charlie", "Engineering", "New York")
            },
            options => options.WithoutStrictOrdering());
    }

    [Fact]
    public async Task CanPerformCrossSourceAggregation()
    {
        // Arrange
        // Inlet for orders containing a customerId and an order total.
        var orders = new Inlet<(int CustomerId, decimal OrderTotal)>();
        // Inlet for customers with customer details.
        var customers = new Inlet<(int CustomerId, string CustomerName)>();

        // Create a flow that:
        // 1. Matches orders.
        // 2. Joins customers on CustomerId.
        // 3. Aggregates the orders per customer.
        // 4. Returns the customer name and the aggregated order total.
        var flow = Pattern.Create()
            .Match(orders, out var customerId, out var orderTotal)
            .Match(customers, customerId, out var customerName)
            .Return(customerName, orderTotal.Sum());

        var topology = new Topology();
        var ordersNode = topology.Intern(orders);
        var customersNode = topology.Intern(customers);
        using var results = topology.Query(flow);

        // Act
        customersNode.Values = new[]
        {
            (1, "Alice"),
            (2, "Bob"),
        };

        ordersNode.Values = new[]
        {
            (1, 50.0m),
            (1, 75.0m),
            (2, 100.0m),
            (1, 25.0m),
        };

        await topology.FlushEffectsAsync();

        // Assert
        results.Should().BeEquivalentTo(
            new[]
            {
                ("Alice", 150.0m),
                ("Bob", 100.0m)
            },
            options => options.WithoutStrictOrdering());
    }

    [Fact]
    public async Task IsLessThan_FiltersCorrectly()
    {
        // Arrange
        var numbers = new Inlet<(int Left, int Right)>();
        var flow = Pattern.Create()
            .Match(numbers, out var left, out var right)
            .IsLessThan(left, right)
            .Return(left, right);

        var topology = new Topology();
        var numbersNode = topology.Intern(numbers);
        using var results = topology.Query(flow);

        // Add test values
        numbersNode.Values = new[]
        {
            (1, 2),   // Valid: 1 < 2
            (3, 3),   // Invalid: 3 is not less than 3
            (4, 2),   // Invalid: 4 is not less than 2
            (5, 10)   // Valid: 5 < 10
        };

        // Act
        await topology.FlushEffectsAsync();

        // Assert
        results.Should().BeEquivalentTo(
            new[]
            {
                (1, 2),
                (5, 10)
            },
            options => options.WithoutStrictOrdering());
    }
}

