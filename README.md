# GreenSwap - Educational Trading Platform

A comprehensive web-based platform for publishing, searching, and purchasing pre-owned items, similar to OLX/Bazar.bg. Built with modern .NET technologies following clean architecture principles.

## 🚀 Technology Stack

- **.NET 8.0** - Latest version of the framework
- **ASP.NET Core MVC** - Web framework with Razor Views
- **SQLite/SQL Server** - Cross-platform database support
- **Entity Framework Core** - ORM with Code First approach
- **ASP.NET Core Identity** - User authentication and authorization
- **SignalR** - Real-time communication for chat
- **xUnit + Moq** - Unit testing framework
- **GitHub Actions** - CI/CD pipeline

## 📁 Project Structure

```
SecondHandGoods/
├── src/
│   ├── SecondHandGoods.Web/         # ASP.NET Core MVC application
│   ├── SecondHandGoods.Data/        # EF Core, entities, and data access
│   └── SecondHandGoods.Services/    # Business logic layer
├── tests/
│   └── SecondHandGoods.Tests/       # Unit tests with xUnit
└── SecondHandGoods.sln              # Solution file
```

### Clean Architecture Layers

- **Web Layer**: Controllers, Views, Models, UI logic
- **Services Layer**: Business rules, application logic, services
- **Data Layer**: Entity Framework context, entities, repositories
- **Tests Layer**: Unit tests covering all layers

## 🏗️ Core Features (Planned)

### User Management
- User registration and authentication
- User profiles with personal information
- Role-based authorization (Admin, User, Moderator)

### Advertisement Management
- Create, edit, and delete advertisements
- Image upload and management
- Categories and condition tracking (New, Used, Damaged, Refurbished)
- Location-based ads

### Search & Filtering ✅
- **Category System**: Visual category browsing with dedicated pages
- **Advanced Search**: Multi-criteria search with comprehensive filters
- **Smart Features**: Autocomplete, suggestions, and saved searches
- **Enhanced Navigation**: Browse dropdowns and quick search widget

### Communication ✅
- **Real-time Chat**: SignalR-powered messaging with typing indicators
- **Message Management**: Inbox, conversation history, and read receipts
- **Integration**: Chat buttons on ads, navigation with unread badges
- **Professional UI**: Modern chat interface with templates and fallbacks

### Rating & Reviews ✅
- **Complete Review System**: User ratings with comprehensive review workflow
- **Order Integration**: Reviews tied to completed transactions
- **Advanced UI**: Interactive star ratings, filtering, and statistics
- **Moderation Tools**: Review reporting, approval, and admin management

### Content Moderation System ✅
- **Automatic Filtering**: Real-time content moderation for advertisements and messages
- **Forbidden Words Management**: Admin interface for managing restricted words and phrases
- **Severity Levels**: Configurable actions (Low/Medium/High/Critical) with blocking or flagging
- **Word Matching**: Support for exact match and partial match detection
- **Content Replacement**: Automatic substitution with appropriate alternatives or censoring
- **Moderation Logs**: Comprehensive audit trail with appeal system support
- **Categories & Analytics**: Organized word management with effectiveness statistics
- **Testing Tools**: Real-time word testing for administrators

### Admin Features ✅
- **Comprehensive Dashboard**: Platform statistics, metrics, and activity monitoring
- **User Management**: Full CRUD operations, role management, and bulk actions
- **Advertisement Moderation**: Content review, featuring, and bulk operations
- **Content Moderation**: Forbidden word filtering, automatic content blocking, and moderation logs
- **Analytics & Reporting**: Category performance, user activity, and platform insights
- **Audit Logging**: Action tracking with reasons and admin notes
- **Modern UI**: Role-based navigation, responsive design, and interactive modals

## 🔧 Development Setup

### Prerequisites
- .NET 8.0 SDK or later
- Visual Studio 2022 or VS Code (SQL Server for production only)

### Getting Started

1. Clone the repository
2. Navigate to the project directory
3. Restore NuGet packages:
   ```bash
   dotnet restore
   ```
4. Build the solution:
   ```bash
   dotnet build
   ```
5. Run the application:
   ```bash
   dotnet run --project src/SecondHandGoods.Web
   ```
6. Navigate to `http://localhost:5036` (see `launchSettings.json` for other profiles)

### Demo Accounts
- **Admin**: `admin@greenswap.com` / `Admin123!` (existing databases may still have the legacy `admin@secondhandgoods.com` until the app migrates it on startup)
- **Demo Users**: 
  - `john.demo@example.com` / `Demo123!`
  - `sarah.demo@example.com` / `Demo123!`
  - `mike.demo@example.com` / `Demo123!`
  - `emily.demo@example.com` / `Demo123!`
  - `alex.demo@example.com` / `Demo123!`

## 🧪 Testing

Run all tests:
```bash
dotnet test
```

**Test Coverage:** 41 unit tests covering:
- Content moderation service (9 tests)
- Controllers (Ads, Admin, Reviews) (8 tests)
- Entity business logic (24 tests)
- Target: ~65% code coverage achieved

Run with coverage:
```bash
dotnet test --collect:"XPlat Code Coverage"
```

## 📦 Deployment

Deployment instructions available for:
- SmarterASP.NET (Primary hosting provider)
- Render
- Heroku

## 🏛️ Architecture Principles

This project follows **SOLID principles** and implements:
- **Dependency Injection** for loose coupling
- **Repository Pattern** for data access abstraction
- **Service Layer Pattern** for business logic encapsulation
- **MVC Pattern** for presentation layer organization

## 📝 Development Progress

- [x] ✅ **Step 1**: Project scaffolding and clean architecture setup
- [x] ✅ **Step 2**: EF Core configuration and database setup (SQLite + SQL Server)
- [x] ✅ **Step 3**: ASP.NET Core Identity implementation
- [x] ✅ **Step 4**: Domain entities and migrations
- [x] ✅ **Step 5**: Comprehensive data seeding and authentication views
- [x] ✅ **Step 6**: Complete advertisement CRUD operations with image management
- [x] ✅ **Step 7**: Enhanced search, filtering, and category browsing system  
- [x] ✅ **Step 8**: SignalR real-time chat with comprehensive messaging features
- [x] ✅ **Step 9**: Comprehensive ratings and reviews system with order management
- [x] ✅ **Step 10**: Complete admin panel with user and advertisement management
- [x] ✅ **Step 11**: Content moderation system with forbidden word filtering
- [x] ✅ **Step 12**: Unit tests with ~65% code coverage (41 tests passing)
- [x] ✅ **Step 13**: CI/CD pipeline with GitHub Actions
- [x] ✅ **Step 14**: Deployment documentation for multiple hosting platforms

## 🤝 Contributing

This is an educational project demonstrating modern .NET development practices. Each commit follows logical incremental changes with clean, production-ready code.

## 📄 License

This project is for educational purposes.