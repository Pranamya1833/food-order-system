# Food Order System

![CI](https://github.com/Pranamya1833/food-order-system/actions/workflows/ci.yml/badge.svg)

A layered C# application for ordering food: browse a menu, build a cart, apply coupons, place orders, and see sales reports. The same business logic runs behind a console app, a WPF desktop app and an ASP.NET Core API.

![Menu screen](docs/menu.png)
![Login screen](docs/login.png)
![Swagger UI](docs/swagger.png)

## Features

- Veg and non-veg menu with search and filters (WPF)
- Cart with quantity controls and coupon codes: `SAVE10`, `FLAT50`, `BIG100`
- Order placement with status tracking (Placed, Preparing, Delivered)
- Sales reports: total revenue, best sellers, revenue per day (LINQ)
- Local login and sign up with salted, hashed passwords (WPF)
- Orders saved to a SQLite database through EF Core, using async/await
- REST API with OpenAPI documentation and Swagger UI
- Optional dish photos: put `1.jpg`, `2.jpg`, and so on in `FoodOrder.Wpf/Images` (the number is the dish id)

## Architecture

| Project | What it holds |
|---|---|
| `FoodOrder.Domain` | Models (`MenuItem`, `Cart`, `Order`), `OrderStatus`, interfaces (`IDiscount`, `IRepository<T>`), custom exceptions |
| `FoodOrder.Services` | `OrderService`, `ReportService` (LINQ reports), `MenuCatalog` and `CouponCatalog` |
| `FoodOrder.Data` | `JsonRepository<T>` and `EfOrderRepository` (EF Core + SQLite) |
| `FoodOrder.App` | Console menu |
| `FoodOrder.Wpf` | WPF desktop interface |
| `FoodOrder.Api` | ASP.NET Core minimal API |
| `FoodOrder.Tests` | Unit tests (xUnit) |
| `FoodOrder.Api.Tests` | API integration tests (xUnit, `WebApplicationFactory`) |

## Design decisions

- **Code depends on interfaces, not classes.** `OrderService` asks for an `IRepository<Order>` through its constructor, and dependency injection decides which implementation it gets.
- **Storage was swapped without touching the services.** Orders were first saved to a JSON file. Moving to EF Core and SQLite meant writing one new repository class and changing one registration line.
- **Discounts use the strategy pattern.** `PercentageDiscount`, `FlatDiscount` and `ThresholdDiscount` all implement `IDiscount`, so new coupon types need no changes to existing code.
- **Repository and service methods are async.** Database calls use `await`, so the WPF window stays responsive and the API doesn't block threads.
- **The API reuses the existing layers.** The API project only adds endpoints and request/response shapes. It doesn't change the Domain, Services or Data projects.
- **Tests use fakes instead of real storage.** Unit tests and API tests replace the repository with an in-memory one, so no test touches a database file.

## API

Run it:

```
dotnet run --project FoodOrder.Api --urls http://localhost:5000
```

Then open http://localhost:5000/swagger to try the endpoints. The raw OpenAPI document is at `/openapi/v1.json`.

| Method | Path | Description |
|---|---|---|
| POST | `/api/auth/register` | Register a user |
| POST | `/api/auth/login` | Login and receive a JWT token |
| GET | `/api/menu` | List menu items |
| GET | `/api/orders` | List orders for the logged-in user (Requires auth) |
| POST | `/api/orders` | Place an order for the logged-in user (Requires auth) |
| GET | `/api/reports/summary` | Order count, revenue and best sellers for logged-in user (Requires auth) |

Example request body for `POST /api/orders`:

```json
{ "itemIds": [1, 2], "coupon": "SAVE10" }
```

Invalid requests (unknown item id, invalid coupon, empty cart) return `400` with a message.

## Testing

```
dotnet test FoodOrder.Tests
dotnet test FoodOrder.Api.Tests
```

- **Unit tests** cover each discount (including boundary cases), cart totals, order status rules, empty-cart handling and order placement through `OrderService` with a fake repository.
- **Integration tests** start the API in memory and check the menu, order placement with a coupon, validation errors and the report summary.

GitHub Actions builds the solution and runs both test projects on every push and pull request.

## How to run

Requires the .NET SDK (the projects target .NET 10). The WPF app needs Windows.

```
dotnet build
dotnet run --project FoodOrder.App    # console
dotnet run --project FoodOrder.Wpf    # desktop UI
dotnet run --project FoodOrder.Api    # API
```

The SQLite database (`order.db`) is created in the folder you run from.

## Known limitations


- The repository and `OrderService` are registered as singletons with an in-memory next-id counter, which isn't safe for concurrent requests. A production version would use a scoped `DbContext` and database-generated ids
- Order status can't be advanced from the UI or the API

## Roadmap

- [x] Make repository and service methods async
- [x] ASP.NET Core API with Swagger
- [x] GitHub Actions to build and test on every push
- [x] JWT login and per-user orders
- [ ] SQL Server support and a Dockerfile